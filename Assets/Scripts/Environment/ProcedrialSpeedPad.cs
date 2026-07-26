using UnityEditor;
using UnityEngine;

public class ProceduralSpeedPad : MonoBehaviour
{
    [Header("Visual Configurations")]
    [SerializeField] private Color padBaseColor = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Color arrowColor = Color.cyan;
    [Tooltip("Change to a negative number if arrows are moving backward!")]
    [SerializeField] private float scrollSpeed = 3f;
    [SerializeField] private int arrowDensity = 5;

    private Material padMaterial;

    private void Start()
    {
        string shaderCode = @"
            Shader ""Custom/ProceduralScrollArrows"" {
                Properties {
                    _BaseColor (""Base Color"", Color) = (0.1, 0.1, 0.1, 1)
                    _ArrowColor (""Arrow Color"", Color) = (0, 1, 1, 1)
                    _Speed (""Scroll Speed"", Float) = 3.0
                    _Density (""Density"", Float) = 5.0
                }
                SubShader {
                    Tags { ""RenderType""=""Opaque"" }
                    Pass {
                        CGPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        #include ""UnityCG.cginc""

                        struct appdata {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };

                        struct v2f {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };

                        fixed4 _BaseColor;
                        fixed4 _ArrowColor;
                        float _Speed;
                        float _Density;

                        v2f vert (appdata v) {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = v.uv;
                            return o;
                        }

                        fixed4 frag (v2f i) : SV_Target {
                            float scrolledY = i.uv.y - (_Time.y * _Speed);
                            
                            // FIX: Added '1.0 -' to flip the arrow texture orientation upside down
                            float repeatedY = 1.0 - frac(scrolledY * _Density);
                            
                            float arrowMask = 0.0;
                            float leftRightMirror = abs(i.uv.x - 0.5) * 2.0; 
                            
                            if (repeatedY > leftRightMirror * 0.5 && repeatedY < (leftRightMirror * 0.5) + 0.3) {
                                arrowMask = 1.0;
                            }

                            return lerp(_BaseColor, _ArrowColor, arrowMask);
                        }
                        ENDCG
                    }
                }
            }";

        Shader runtimeShader = ShaderUtil.CreateShaderAsset(shaderCode);
        if (runtimeShader != null)
        {
            padMaterial = new Material(runtimeShader);
        }
        else
        {
            padMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = padMaterial;
        }
    }

    private void Update()
    {
        if (padMaterial != null)
        {
            padMaterial.SetColor("_BaseColor", padBaseColor);
            padMaterial.SetColor("_ArrowColor", arrowColor);
            padMaterial.SetFloat("_Speed", scrollSpeed);
            padMaterial.SetFloat("_Density", arrowDensity);
        }
    }
}