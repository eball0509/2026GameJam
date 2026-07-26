Shader "Custom/ProceduralScrollArrows" {
    Properties {
        _BaseColor ("Base Color", Color) = (0.1, 0.1, 0.1, 1)
        _ArrowColor ("Arrow Color", Color) = (0, 1, 1, 1)
        _Speed ("Scroll Speed", Float) = 3.0
        _Density ("Density", Float) = 5.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
                // Animate UV over time based on speed
                float scrolledY = i.uv.y - (_Time.y * _Speed);
                
                // Keep repeating the pattern cleanly across the mesh surface
                float repeatedY = 1.0 - frac(scrolledY * _Density);
                
                float arrowMask = 0.0;
                float leftRightMirror = abs(i.uv.x - 0.5) * 2.0; 
                
                // Form the chevron shape logic
                if (repeatedY > leftRightMirror * 0.5 && repeatedY < (leftRightMirror * 0.5) + 0.3) {
                    arrowMask = 1.0;
                }

                return lerp(_BaseColor, _ArrowColor, arrowMask);
            }
            ENDCG
        }
    }
}