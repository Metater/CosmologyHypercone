// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using UnityEngine.Rendering;

namespace Pcx
{
    /// A renderer class that renders a point cloud contained by PointCloudData.
    [ExecuteInEditMode]
    public sealed class PointCloudRenderer : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] PointCloudData _sourceData = null;

        public PointCloudData sourceData {
            get { return _sourceData; }
            set { _sourceData = value; }
        }

        [SerializeField] Color _pointTint = new Color(0.5f, 0.5f, 0.5f, 1);

        public Color pointTint {
            get { return _pointTint; }
            set { _pointTint = value; }
        }

        [SerializeField] float _pointSize = 0.05f;

        public float pointSize {
            get { return _pointSize; }
            set { _pointSize = value; }
        }

        #endregion

        #region Public properties (nonserialized)

        public ComputeBuffer sourceBuffer { get; set; }

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] Shader _pointShader = null;
        [SerializeField, HideInInspector] Shader _diskShader = null;

        #endregion

        #region Private objects

        Material _pointMaterial;
        Material _diskMaterial;
        Mesh _pointMesh;
        int _pointMeshCount = -1;
        int _pointMeshDataVersion = -1;

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _pointSize = Mathf.Max(0, _pointSize);
        }

        void OnDestroy()
        {
            if (_pointMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_pointMaterial);
                    Destroy(_diskMaterial);
                }
                else
                {
                    DestroyImmediate(_pointMaterial);
                    DestroyImmediate(_diskMaterial);
                }
            }

            if (_pointMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_pointMesh);
                }
                else
                {
                    DestroyImmediate(_pointMesh);
                }
            }
        }

        bool TryUpdatePointMesh()
        {
            if (_sourceData == null || _sourceData.pointCount == 0) return false;

            if (_pointMesh == null)
            {
                _pointMesh = new Mesh();
                _pointMesh.name = "PointCloudRenderer Mesh";
            }

            if (_pointMeshCount != _sourceData.pointCount ||
                _pointMeshDataVersion != _sourceData.dataVersion)
            {
                if (!_sourceData.TryGetPointData(out var positions, out var colors)) return false;

                _pointMesh.Clear();
                _pointMesh.indexFormat = positions.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                _pointMesh.vertices = positions;
                _pointMesh.colors = colors;

                var indices = new int[positions.Length];
                for (var i = 0; i < indices.Length; i++) indices[i] = i;

                _pointMesh.SetIndices(indices, MeshTopology.Points, 0, false);

                _pointMeshCount = positions.Length;
                _pointMeshDataVersion = _sourceData.dataVersion;
            }

            return _pointMeshCount > 0;
        }

        public void OnRenderObject()
        {
            // We need a source data or an externally given buffer.
            if (_sourceData == null && sourceBuffer == null) return;

            // Check the camera condition.
            var camera = Camera.current;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (camera.name == "Preview Scene Camera") return;

            // TODO: Do view frustum culling here.

            // return if computer buffer has zero points
            if (_sourceData != null && _sourceData.pointCount == 0) return;

            // Lazy initialization
            if (_pointMaterial == null)
            {
                _pointMaterial = new Material(_pointShader);
                _pointMaterial.hideFlags = HideFlags.DontSave;
                _pointMaterial.EnableKeyword("_COMPUTE_BUFFER");

                _diskMaterial = new Material(_diskShader);
                _diskMaterial.hideFlags = HideFlags.DontSave;
                _diskMaterial.EnableKeyword("_COMPUTE_BUFFER");
            }

            var useMesh = Application.platform == RuntimePlatform.WebGLPlayer;
            if (useMesh)
            {
                if (!TryUpdatePointMesh()) return;

                _pointMaterial.DisableKeyword("_COMPUTE_BUFFER");
                _pointMaterial.SetPass(0);
                _pointMaterial.SetColor("_Tint", _pointTint);
                _pointMaterial.SetFloat("_PointSize", _pointSize);
                Graphics.DrawMeshNow(_pointMesh, transform.localToWorldMatrix);
                return;
            }

            // Use the external buffer if given any.
            var pointBuffer = sourceBuffer != null ?
                sourceBuffer : _sourceData.computeBuffer;

            var useDisk = _pointSize > 0 && Application.platform != RuntimePlatform.WebGLPlayer;

            if (!useDisk)
            {
                _pointMaterial.SetPass(0);
                _pointMaterial.SetColor("_Tint", _pointTint);
                _pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                _pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
                _pointMaterial.SetFloat("_PointSize", _pointSize);
                #if UNITY_2019_1_OR_NEWER
                Graphics.DrawProceduralNow(MeshTopology.Points, pointBuffer.count, 1);
                #else
                Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
                #endif
            }
            else
            {
                _diskMaterial.SetPass(0);
                _diskMaterial.SetColor("_Tint", _pointTint);
                _diskMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                _diskMaterial.SetBuffer("_PointBuffer", pointBuffer);
                _diskMaterial.SetFloat("_PointSize", pointSize);
                #if UNITY_2019_1_OR_NEWER
                Graphics.DrawProceduralNow(MeshTopology.Points, pointBuffer.count, 1);
                #else
                Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
                #endif
            }
        }

        #endregion
    }
}
