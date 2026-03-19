Shader "Custom/Line"
{
    Properties
    {
        // 中心颜色（黄色）
        _CenterColor ("Center Color", Color) = (1, 0.8, 0, 1)
        // 边缘颜色（橙色）
        _EdgeColor ("Edge Color", Color) = (1, 0.4, 0, 1)
        // 渐变强度（控制边缘渐变范围）
        _GradientRange ("Gradient Range", Range(0, 0.5)) = 0.2
        // 整体透明度（兼容代码控制）
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        // 透明队列 + 关闭深度写入（避免遮挡）
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha // 透明混合模式
        ZWrite Off // 关闭深度写入，防止光线遮挡其他物体

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _CenterColor;
            fixed4 _EdgeColor;
            float _GradientRange;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 计算当前UV到中心(0.5)的距离
                float distanceToCenter = abs(i.uv.y - 0.5);
                
                // 计算渐变权重：中心=0（纯黄色），边缘=1（纯橙色）
                float gradientWeight = smoothstep(0, _GradientRange, distanceToCenter);
                
                // 混合中心色和边缘色
                fixed4 col = lerp(_CenterColor, _EdgeColor, gradientWeight);
                
                // 应用整体透明度
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
}