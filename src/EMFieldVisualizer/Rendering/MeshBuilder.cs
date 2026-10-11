using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Rendering;

/// <summary>Accumulates simple primitives into a single MeshGeometry3D.</summary>
public sealed class MeshBuilder
{
    private const int ArrowSides = 6;

    private readonly Point3DCollection _positions;
    private readonly Int32Collection _indices;
    private readonly PointCollection _texture;
    private Vector3DCollection? _normals;

    public MeshBuilder(int estimatedVertices = 256)
    {
        _positions = new Point3DCollection(estimatedVertices);
        _indices = new Int32Collection(estimatedVertices * 3);
        _texture = new PointCollection(estimatedVertices);
    }

    public int VertexCount => _positions.Count;

    private int AddVertex(Point3D p, double u)
    {
        _positions.Add(p);
        _texture.Add(new Point(u, 0.5));
        return _positions.Count - 1;
    }

    private void Tri(int a, int b, int c)
    {
        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);
    }

    private static void Basis(Vector3D d, out Vector3D u, out Vector3D v)
    {
        var a = Math.Abs(d.X) < 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
        u = Vector3D.CrossProduct(d, a);
        u.Normalize();
        v = Vector3D.CrossProduct(d, u);
    }

    /// <summary>Arrow from <paramref name="start"/> along <paramref name="vector"/>. <paramref name="u"/> is the texture coordinate (colour).</summary>
    public void AddArrow(Point3D start, Vector3D vector, double radius, double u = 0.5)
    {
        double len = vector.Length;
        if (len < 1e-9) return;
        var d = vector / len;
        Basis(d, out var bu, out var bv);

        double r = Math.Min(radius, len * 0.12);
        double headLen = Math.Min(len * 0.4, r * 7);
        double headR = r * 2.4;
        var shaftEnd = start + d * (len - headLen);
        var tip = start + vector;

        int baseIndex = _positions.Count;
        for (int i = 0; i < ArrowSides; i++)
        {
            double a = 2 * Math.PI * i / ArrowSides;
            var off = bu * Math.Cos(a) + bv * Math.Sin(a);
            AddVertex(start + off * r, u);
            AddVertex(shaftEnd + off * r, u);
        }
        for (int i = 0; i < ArrowSides; i++)
        {
            int j = (i + 1) % ArrowSides;
            int b0 = baseIndex + 2 * i, t0 = b0 + 1;
            int b1 = baseIndex + 2 * j, t1 = b1 + 1;
            Tri(b0, b1, t1);
            Tri(b0, t1, t0);
        }

        int ring = _positions.Count;
        for (int i = 0; i < ArrowSides; i++)
        {
            double a = 2 * Math.PI * i / ArrowSides;
            AddVertex(shaftEnd + (bu * Math.Cos(a) + bv * Math.Sin(a)) * headR, u);
        }
        int tipIndex = AddVertex(tip, u);
        int back = AddVertex(shaftEnd, u);
        for (int i = 0; i < ArrowSides; i++)
        {
            int j = (i + 1) % ArrowSides;
            Tri(ring + i, ring + j, tipIndex);
            Tri(ring + j, ring + i, back);
        }
    }

    public void AddCylinder(Point3D a, Point3D b, double radius, int sides = 12, double u = 0.5)
    {
        var axis = b - a;
        if (axis.Length < 1e-9) return;
        var d = axis;
        d.Normalize();
        Basis(d, out var bu, out var bv);
        int baseIndex = _positions.Count;
        for (int i = 0; i < sides; i++)
        {
            double ang = 2 * Math.PI * i / sides;
            var off = (bu * Math.Cos(ang) + bv * Math.Sin(ang)) * radius;
            AddVertex(a + off, u);
            AddVertex(b + off, u);
        }
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            int b0 = baseIndex + 2 * i, t0 = b0 + 1;
            int b1 = baseIndex + 2 * j, t1 = b1 + 1;
            Tri(b0, b1, t1);
            Tri(b0, t1, t0);
        }
    }

    public void AddBox(Point3D min, Point3D max)
    {
        var p = new Point3D[8];
        for (int i = 0; i < 8; i++)
            p[i] = new Point3D((i & 1) == 0 ? min.X : max.X, (i & 2) == 0 ? min.Y : max.Y, (i & 4) == 0 ? min.Z : max.Z);
        int[][] faces = [[0, 2, 3, 1], [4, 5, 7, 6], [0, 1, 5, 4], [2, 6, 7, 3], [0, 4, 6, 2], [1, 3, 7, 5]];
        foreach (var f in faces)
        {
            int s = _positions.Count;
            foreach (int idx in f) AddVertex(p[idx], 0.5);
            Tri(s, s + 1, s + 2);
            Tri(s, s + 2, s + 3);
        }
    }

    public void AddSphere(Point3D center, double radius, int slices = 16, int stacks = 10)
    {
        _normals ??= new Vector3DCollection();
        while (_normals.Count < _positions.Count) _normals.Add(new Vector3D());
        int baseIndex = _positions.Count;
        for (int i = 0; i <= stacks; i++)
        {
            double theta = Math.PI * i / stacks;
            for (int j = 0; j <= slices; j++)
            {
                double phi = 2 * Math.PI * j / slices;
                var n = new Vector3D(Math.Sin(theta) * Math.Cos(phi), Math.Cos(theta), Math.Sin(theta) * Math.Sin(phi));
                AddVertex(center + n * radius, 0.5);
                _normals.Add(n);
            }
        }
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                int a = baseIndex + i * (slices + 1) + j;
                int b = a + slices + 1;
                Tri(a, a + 1, b);
                Tri(a + 1, b + 1, b);
            }
        }
    }

    public MeshGeometry3D ToMesh(bool freeze = true)
    {
        var mesh = new MeshGeometry3D
        {
            Positions = _positions,
            TriangleIndices = _indices,
            TextureCoordinates = _texture,
        };
        if (_normals != null && _normals.Count == _positions.Count) mesh.Normals = _normals;
        if (freeze) mesh.Freeze();
        return mesh;
    }

    // ---- Materials ----

    public static Material SolidMaterial(Color color, double emissive = 0.25, bool specular = true)
    {
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
        if (specular) group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 40));
        if (emissive > 0)
            group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb((byte)(emissive * 255), color.R, color.G, color.B))));
        group.Freeze();
        return group;
    }

    /// <summary>Material whose colour follows the u texture coordinate (0..1) through the given stops.</summary>
    public static Material GradientMaterial(params Color[] stops)
    {
        LinearGradientBrush Make(byte alpha)
        {
            var brush = new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
            };
            for (int i = 0; i < stops.Length; i++)
            {
                var c = stops[i];
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, c.R, c.G, c.B), i / (double)(stops.Length - 1)));
            }
            return brush;
        }

        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(Make(255)));
        group.Children.Add(new EmissiveMaterial(Make(110)));
        group.Freeze();
        return group;
    }
}
