using Pcx;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatePointData : MonoBehaviour
{
    public Slider zw, yw, yz, xw, xz, xy;
    public Slider fourthDimMin, fourthDimMax;
    public TMP_Dropdown equation, projection, coloring;

    public int numPoints = 1000;
    public TMP_Dropdown dropdown;
    public float minValue = -1f;
    public float maxValue = 1f;
    public float threshold = 0.01f;
    public float scale = 1f;

    private PointCloudData data;
    private PointCloudRenderer rend;

    private void Awake()
    {
        data = GetComponent<PointCloudData>();
        rend = GetComponent<PointCloudRenderer>();
    }

    private void OnEnable()
    {
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        equation.onValueChanged.AddListener(OnDropdownValueChanged);
        projection.onValueChanged.AddListener(OnDropdownValueChanged);
        coloring.onValueChanged.AddListener(OnDropdownValueChanged);
        zw.onValueChanged.AddListener(OnSliderChanged);
        yw.onValueChanged.AddListener(OnSliderChanged);
        yz.onValueChanged.AddListener(OnSliderChanged);
        xw.onValueChanged.AddListener(OnSliderChanged);
        xz.onValueChanged.AddListener(OnSliderChanged);
        xy.onValueChanged.AddListener(OnSliderChanged);
        fourthDimMin.onValueChanged.AddListener(OnMinSliderChanged);
        fourthDimMax.onValueChanged.AddListener(OnMaxSliderChanged);
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        equation.onValueChanged.RemoveListener(OnDropdownValueChanged);
        projection.onValueChanged.RemoveListener(OnDropdownValueChanged);
        coloring.onValueChanged.RemoveListener(OnDropdownValueChanged);
        zw.onValueChanged.RemoveListener(OnSliderChanged);
        yw.onValueChanged.RemoveListener(OnSliderChanged);
        yz.onValueChanged.RemoveListener(OnSliderChanged);
        xw.onValueChanged.RemoveListener(OnSliderChanged);
        xz.onValueChanged.RemoveListener(OnSliderChanged);
        xy.onValueChanged.RemoveListener(OnSliderChanged);
        fourthDimMin.onValueChanged.RemoveListener(OnMinSliderChanged);
        fourthDimMax.onValueChanged.RemoveListener(OnMaxSliderChanged);
    }

    private void OnDropdownValueChanged(int collapseEnum)
    {
        Generate();
    }

    private void OnSliderChanged(float _)
    {
        Generate();
    }

    private void OnMinSliderChanged(float _)
    {
        const float minGap = 0.01f;
        float minValuePercent = fourthDimMin.value;
        float maxValuePercent = 1 - fourthDimMax.value;

        if (minValuePercent >= maxValuePercent - minGap)
        {
            maxValuePercent = Mathf.Clamp01(minValuePercent + minGap);
            fourthDimMax.value = 1 - maxValuePercent;
        }

        Generate();
    }

    private void OnMaxSliderChanged(float _)
    {
        const float minGap = 0.01f;
        float minValuePercent = fourthDimMin.value;
        float maxValuePercent = 1 - fourthDimMax.value;

        if (maxValuePercent <= minValuePercent + minGap)
        {
            minValuePercent = Mathf.Clamp01(maxValuePercent - minGap);
            fourthDimMin.value = minValuePercent;
        }

        Generate();
    }

    private void OnToggleChanged(bool _)
    {
        Generate();
    }

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        int collapseEnum = dropdown.value;
        float zwVal = zw.value * 2 * Mathf.PI;
        float ywVal = yw.value * 2 * Mathf.PI;
        float yzVal = yz.value * 2 * Mathf.PI;
        float xwVal = xw.value * 2 * Mathf.PI;
        float xzVal = xz.value * 2 * Mathf.PI;
        float xyVal = xy.value * 2 * Mathf.PI;
        float minValuePercent = fourthDimMin.value;
        float maxValuePercent = 1 - fourthDimMax.value;

        Matrix4x4 matrix = GetMatrix(zwVal, ywVal, yzVal, xwVal, xzVal, xyVal);

        //var count = 1000;
        //var points = new Vector3[count];
        //var colors = new Color32[count];
        //for (var i = 0; i < count; i++)
        //{
        //    points[i] = Random.insideUnitSphere * 5;
        //    colors[i] = new Color32(
        //        (byte)Random.Range(0, 256),
        //        (byte)Random.Range(0, 256),
        //        (byte)Random.Range(0, 256),
        //        255
        //    );
        //}

        //data.Initialize(points.ToList(), colors.ToList());

        // scale the point count based on percentage of the range of the fourth dimension that is visible
        int pts = (int)(numPoints / (maxValuePercent - minValuePercent));

        Hypercone hypercone = new(pts, minValue, maxValue, threshold);

        hypercone.Transform(matrix);
        hypercone.Scale(scale);

        var (remaining, collapsed) = hypercone.Collapse(collapseEnum);
        float minCollapsed = collapsed.Min();
        float maxCollapsed = collapsed.Max();

        for (int i = remaining.Count - 1; i >= 0; i--)
        {
            float collapsedT = Mathf.InverseLerp(minCollapsed, maxCollapsed, collapsed[i]);
            if (collapsedT < minValuePercent || collapsedT > maxValuePercent)
            {
                remaining.RemoveAt(i);
                collapsed.RemoveAt(i);
            }
        }

        float newMinCollapsed = collapsed.Min();
        float newMaxCollapsed = collapsed.Max();

        float minX = remaining.Min(x => x.x);
        float maxX = remaining.Max(x => x.x);
        float minY = remaining.Min(x => x.y);
        float maxY = remaining.Max(x => x.y);
        float minZ = remaining.Min(x => x.z);
        float maxZ = remaining.Max(x => x.z);

        List<Color32> colors = new();

        bool colorBy3DPoint = coloring.value == 0;

        if (colorBy3DPoint)
        {
            for (var i = 0; i < remaining.Count; i++)
            {
                float r = Mathf.InverseLerp(minX, maxX, remaining[i].x);
                float g = Mathf.InverseLerp(minY, maxY, remaining[i].y);
                float b = Mathf.InverseLerp(minZ, maxZ, remaining[i].z);
                colors.Add(new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255));
            }
        }
        else
        {
            for (var i = 0; i < remaining.Count; i++)
            {
                float t = Mathf.InverseLerp(newMinCollapsed, newMaxCollapsed, collapsed[i]);
                colors.Add(GetColor(t));
            }
        }

        data.Initialize(remaining, colors);

        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    private Color32 GetColor(float t)
    {
        // lerp between blue and green based on t
        return Color32.Lerp(new Color32(0, 0, 255, 255), new Color32(0, 255, 0, 255), t);
    }

    private class Hypercone
    {
        private readonly List<Vector4> points = new();
        private float threshold;

        public void Transform(Matrix4x4 matrix)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector4 p = points[i];

                points[i] = new Vector4(
                    matrix.m00 * p.x + matrix.m01 * p.y + matrix.m02 * p.z + matrix.m03 * p.w,
                    matrix.m10 * p.x + matrix.m11 * p.y + matrix.m12 * p.z + matrix.m13 * p.w,
                    matrix.m20 * p.x + matrix.m21 * p.y + matrix.m22 * p.z + matrix.m23 * p.w,
                    matrix.m30 * p.x + matrix.m31 * p.y + matrix.m32 * p.z + matrix.m33 * p.w
                );
            }
        }

        public void Scale(float scale)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector4 p = points[i];
                points[i] = p * scale;
            }
        }


        public (List<Vector3> remaining, List<float> collapsed) Collapse(int collapseEnum)
        {
            List<Vector3> remaining = new();
            List<float> collapsed = new();

            switch (collapseEnum)
            {
                case 0: // Collapse X (W, Y, Z)
                    foreach (var point in points)
                    {
                        remaining.Add(new Vector3(point.w, point.y, point.z));
                        collapsed.Add(point.x);
                    }
                    break;
                case 1: // Collapse Y (X, W, Z)
                    foreach (var point in points)
                    {
                        remaining.Add(new Vector3(point.x, point.w, point.z));
                        collapsed.Add(point.y);
                    }
                    break;
                case 2: // Collapse Z (X, Y, W)
                    foreach (var point in points)
                    {
                        remaining.Add(new Vector3(point.x, point.y, point.w));
                        collapsed.Add(point.z);
                    }
                    break;
                case 3: // Collapse W (X, Y, Z)
                    foreach (var point in points)
                    {
                        remaining.Add(new Vector3(point.x, point.y, point.z));
                        collapsed.Add(point.w);
                    }
                    break;
            }

            return (remaining, collapsed);
        }

        public Hypercone(int n, float minValue, float maxValue, float threshold)
        {
            this.threshold = threshold;
            for (var i = 0; i < n; i++)
            {
                var point = new Vector4(
                    Random.Range(minValue, maxValue),
                    Random.Range(minValue, maxValue), Random.Range(minValue, maxValue), Random.Range(minValue, maxValue)
                );

                if (IsInside(point))
                {
                    points.Add(point);
                }
            }

            //points = GeneratePoints(n);
        }

        private static List<Vector4> GeneratePoints(int n)
        {
            // generate points that only satisfy w^2 = x^2 + y^2 + z^2
            var points = new List<Vector4>();
            for (var i = 0; i < n; i++)
            {
                var v = Random.insideUnitSphere;
                var w = v.magnitude * (Random.value < 0.5f ? -1f : 1f);
                points.Add(new Vector4(v.x, v.y, v.z, w));
            }

            return points;
        }

        private bool IsInside(Vector4 point)
        {
            float wS = point.w * point.w;
            float xS = point.x * point.x;
            float yS = point.y * point.y;
            float zS = point.z * point.z;

            return wS <= xS + yS + zS;

            // check if the point is within the threshold distance from the surface of the hypercone
            //float val = xS + yS + zS;
            //float diff = Mathf.Abs(wS - val);
            //return diff < threshold;
        }
    }

    private static Matrix4x4 GetMatrix(float zw, float yw, float yz, float xw, float xz, float xy)
    {
        float c12 = Mathf.Cos(xy);
        float s12 = Mathf.Sin(xy);

        float c13 = Mathf.Cos(xz);
        float s13 = Mathf.Sin(xz);

        float c14 = Mathf.Cos(xw);
        float s14 = Mathf.Sin(xw);

        float c23 = Mathf.Cos(yz);
        float s23 = Mathf.Sin(yz);

        float c24 = Mathf.Cos(yw);
        float s24 = Mathf.Sin(yw);

        float c34 = Mathf.Cos(zw);
        float s34 = Mathf.Sin(zw);

        // R_xy
        Matrix4x4 Rxy = Matrix4x4.identity;
        Rxy.m00 = c12; Rxy.m01 = -s12;
        Rxy.m10 = s12; Rxy.m11 = c12;

        // R_xz
        Matrix4x4 Rxz = Matrix4x4.identity;
        Rxz.m00 = c13; Rxz.m02 = -s13;
        Rxz.m20 = s13; Rxz.m22 = c13;

        // R_xw
        Matrix4x4 Rxw = Matrix4x4.identity;
        Rxw.m00 = c14; Rxw.m03 = -s14;
        Rxw.m30 = s14; Rxw.m33 = c14;

        // R_yz
        Matrix4x4 Ryz = Matrix4x4.identity;
        Ryz.m11 = c23; Ryz.m12 = -s23;
        Ryz.m21 = s23; Ryz.m22 = c23;

        // R_yw
        Matrix4x4 Ryw = Matrix4x4.identity;
        Ryw.m11 = c24; Ryw.m13 = -s24;
        Ryw.m31 = s24; Ryw.m33 = c24;

        // R_zw
        Matrix4x4 Rzw = Matrix4x4.identity;
        Rzw.m22 = c34; Rzw.m23 = -s34;
        Rzw.m32 = s34; Rzw.m33 = c34;

        // M = Rzw � Ryw � Ryz � Rxw � Rxz � Rxy
        Matrix4x4 matrix =
            Rzw * Ryw * Ryz * Rxw * Rxz * Rxy;

        return matrix;
    }

}
