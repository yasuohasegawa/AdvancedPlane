using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedMesh
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct PlaneVertex
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector4 tangent;
        public Color32 color;
        public Vector2 uv;
        public Vector2 uv2;
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AdvancedPlane : MonoBehaviour
    {
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private float scale = 1;
        [SerializeField] private bool useDepth = false;
        [SerializeField] private bool updateMesh = false;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private VertexAttributeDescriptor[] _layout = new[]
        {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32, 2),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32, 2),
    };

        private bool _isGenerated = false;

        NativeArray<int> _indexBuffer;
        NativeArray<PlaneVertex> _vertexBuffer;

        // Start is called before the first frame update
        void Start()
        {
            InitializeMesh();
            _ = AllocateBufferAsync(GenerateMesh);
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (updateMesh) DisposeBuffers();
        }

        private void InitializeMesh()
        {
            _mesh = new Mesh();
            _meshFilter = GetComponent<MeshFilter>();
            _meshFilter.mesh = _mesh;
        }

        private async Task AllocateBufferAsync(System.Action mainCallback)
        {
            _isGenerated = false;
            SynchronizationContext context = SynchronizationContext.Current;
            await Task.Run(() =>
            {
                var vertexCount = height * width;
                _vertexBuffer = new NativeArray<PlaneVertex>(vertexCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

                int indexCount = vertexCount * 6;
                _indexBuffer = new NativeArray<int>(indexCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

                int index = 0;
                int indicesCount = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PlaneVertex pv = new PlaneVertex();
                        float w = width * scale;
                        float h = height * scale;
                        float vx = x * scale;
                        float vy = y * scale;
                        float startX = ((float)(w - (1 * scale)) * 0.5f);
                        float startY = ((float)(h - (1 * scale)) * 0.5f);
                        pv.pos = new Vector3(vx - startX, (useDepth) ? 0 : vy - startY, (useDepth) ? vy - startY : 0);

                        float u = x / (float)(width - 1);
                        float v = y / (float)(height - 1);

                        pv.uv = new Vector2(u, v);
                        _vertexBuffer[index] = pv;

                        // Reference for how to create indexes
                        // https://forum.lookingglassfactory.com/t/mesh-generation-in-unity/241
                        if (x != 0 && y != 0)
                        {
                            int v4 = x + y * width;
                            int v3 = v4 - 1;
                            int v1 = v4 - width;
                            int v0 = v3 - width;

                            _indexBuffer[indicesCount * 6] = v0;
                            _indexBuffer[indicesCount * 6 + 1] = v3;
                            _indexBuffer[indicesCount * 6 + 2] = v4;
                            _indexBuffer[indicesCount * 6 + 3] = v4;
                            _indexBuffer[indicesCount * 6 + 4] = v1;
                            _indexBuffer[indicesCount * 6 + 5] = v0;
                            indicesCount++;
                        }

                        index++;
                    }
                }
            });

            context.Post(state => {
                mainCallback?.Invoke();
                context = null;
            }, null);
        }

        private void DisposeBuffers()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        private void GenerateMesh()
        {
            var vertexCount = height * width;
            _mesh.SetVertexBufferParams(vertexCount, _layout);

            int indexCount = vertexCount * 6;
            _mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

            _mesh.SetVertexBufferData(_vertexBuffer, 0, 0, vertexCount);
            _mesh.SetIndexBufferData(_indexBuffer, 0, 0, indexCount);

            // Submesh definition
            var meshDesc = new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles);
            _mesh.subMeshCount = 1;
            _mesh.SetSubMesh(0, meshDesc);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            if (!updateMesh)
            {
                DisposeBuffers();
            }

            _isGenerated = true;
        }

        public void UpdateMesh(System.Action<NativeArray<PlaneVertex>, int> update = null)
        {
            if (updateMesh && _isGenerated)
            {
                var vertexCount = height * width;
                update?.Invoke(_vertexBuffer, vertexCount);
                _mesh.SetVertexBufferData(_vertexBuffer, 0, 0, vertexCount);
                _mesh.RecalculateNormals();
            }
        }

        [ContextMenu("SaveMesh")]
        public void SaveMesh()
        {
            InitializeMesh();
            _ = AllocateBufferAsync(() => {
                GenerateMesh();
                AssetDatabase.CreateAsset(_mesh, "Assets/AdvancedPlane.asset");
            });
        }
    }
}
