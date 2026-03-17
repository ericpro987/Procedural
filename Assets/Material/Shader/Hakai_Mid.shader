Shader "Custom/Hakai"
{
    Properties
    {
        [HDR] _Color ("Energy Color", Color) = (0, 0.5, 1, 1)        
        [HDR] _Color2 ("Energy Color2", Color) = (0, 0.5, 1, 1)

        _MainTex ("Noise Texture", 2D) = "white" {}
        _Speed ("Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _SizeBall_Center ("SizeBall_Center", Range(0,1)) = 1
    }
    SubShader
    {
        // Configuración para transparencia y brillo aditivo
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One // Mezcla Aditiva: suma el color al fondo
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float SizeBall_Center;
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Color2;
            float4 _Speed;
            float _FresnelPower;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Cálculo de dirección de vista y normales para el Fresnel
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 1. Animación de la textura de ruido
                float2 animatedUV = i.uv + _Speed.xy * _Time.y * 2;
                fixed4 noise = tex2D(_MainTex, animatedUV);
            //    clip(noise.rgb - _SizeBall_Center)
                // 2. Efecto Fresnel (brillo en los bordes)
                float fresnel = pow(3.0 - saturate(dot(normalize(i.worldNormal), i.viewDir)), _FresnelPower);

                // 3. Combinación final
                fixed4 finalColor = _Color * noise * fresnel;
                fixed4 finalColor2 = _Color2 * noise * fresnel;
                fixed4 finalColor3 = _Color / _Color2;

                return finalColor3;
            }
            ENDCG
        }
    }
}