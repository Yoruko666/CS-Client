Shader "Custom/ImprovedDecalShader" {
    Properties {
        _MainTex ("贴花纹理", 2D) = "white" {}
        _Color ("贴花颜色", Color) = (1,1,1,1)
        _DecalPosition ("贴花位置", Vector) = (0,0,0,1)
        _DecalRotation ("贴花旋转(角度)", Vector) = (0,0,0,0)
        _DecalScale ("贴花缩放", Vector) = (1,1,1,1)
        _DecalNormal ("贴花法线方向", Vector) = (0,1,0,0)
        _DecalOpacity ("贴花透明度", Range(0,1)) = 1.0
        _DebugColor ("调试颜色", Color) = (1,0,0,0.5)
        _DebugMode ("调试模式", Int) = 0 // 0=正常,1=显示范围,2=显示法线方向
    }

    SubShader {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // 关闭背面剔除，确保贴花两面都能看到

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _DecalPosition;
            float4 _DecalRotation;
            float4 _DecalScale;
            float4 _DecalNormal;
            float _DecalOpacity;
            float4 _DebugColor;
            int _DebugMode;

            // 旋转矩阵函数
            float3x3 RotateX(float angle) {
                float rad = angle * UNITY_PI / 180.0;
                float c = cos(rad);
                float s = sin(rad);
                return float3x3(1, 0, 0, 0, c, -s, 0, s, c);
            }

            float3x3 RotateY(float angle) {
                float rad = angle * UNITY_PI / 180.0;
                float c = cos(rad);
                float s = sin(rad);
                return float3x3(c, 0, s, 0, 1, 0, -s, 0, c);
            }

            float3x3 RotateZ(float angle) {
                float rad = angle * UNITY_PI / 180.0;
                float c = cos(rad);
                float s = sin(rad);
                return float3x3(c, -s, 0, s, c, 0, 0, 0, 1);
            }

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 计算从贴花位置到像素位置的向量
                float3 dir = i.worldPos - _DecalPosition.xyz;
                
                // 标准化法线方向
                float3 normal = normalize(_DecalNormal.xyz);
                
                // 计算与贴花法线的点积
                float dotProd = dot(dir, normal);
                
                // 调试模式1: 显示贴花影响范围
                if (_DebugMode == 1) {
                    return _DebugColor;
                }
                
                // 调试模式2: 显示法线方向
                if (_DebugMode == 2) {
                    return float4(normal * 0.5 + 0.5, 1);
                }
                
                // 如果在后方，不显示贴花 (增加容差)
                if (dotProd > 0.01) {
                    discard;
                }
                
                // 创建贴花的坐标系 (更健壮的切线计算)
                float3 tangent;
                if (abs(normal.y) < 0.999) {
                    tangent = normalize(cross(normal, float3(0,1,0)));
                } else {
                    tangent = normalize(cross(normal, float3(1,0,0)));
                }
                float3 binormal = cross(normal, tangent);
                
                // 构建旋转矩阵
                float3x3 rotationMatrix = RotateZ(_DecalRotation.z) * 
                                         RotateY(_DecalRotation.y) * 
                                         RotateX(_DecalRotation.x);
                
                // 转换到贴花局部空间
                float3x3 decalToWorld = float3x3(tangent, binormal, normal);
                float3x3 worldToDecal = transpose(decalToWorld);
                float3 localPos = mul(worldToDecal, dir);
                localPos = mul(rotationMatrix, localPos);
                
                // 应用缩放 (处理可能的零值)
                float2 scale = max(_DecalScale.xy, 0.001);
                float2 uv = localPos.xy / scale + 0.5;
                
                // 检查是否在贴花范围内 (增加边缘柔和度)
                float2 mask = smoothstep(0, 0.01, uv) * smoothstep(1, 0.99, uv);
                if (mask.x * mask.y < 0.01) {
                    discard;
                }
                
                // 采样贴花纹理
                fixed4 col = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw) * _Color;
                col.a *= _DecalOpacity * mask.x * mask.y;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
    