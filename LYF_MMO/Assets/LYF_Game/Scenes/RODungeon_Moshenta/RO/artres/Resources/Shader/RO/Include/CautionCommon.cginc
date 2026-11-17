#ifndef CAUTION_COMMON_INCLUDE
#define CAUTION_COMMON_INCLUDE

#include "UnityCG.cginc"

struct v2f
{    
    float4 vertex : SV_POSITION;
    float2 pos : TEXCOORD0; // -1 ~ 1
    #if defined(_SHAPE_FAN)
    float angle : TEXCOORD1;
    #endif
};

sampler2D _Decal;
half4 _Color;
half4 _CauColor;
half4 _Border;
half4 _Params;
half4 _ParamsEx;
half _Progress2;
half _Opacity;

#if defined(_FLOW_AUTO)
half4 _ParamsFlow;
#elif defined(_FLOW_MANUAL)
half _Progress;
#endif

#if defined(_Caution2)
    sampler2D _BaseTex;
    half4 _BaseColor;
    float _CenterOffset;
    float _FadeCo;
#endif

//==========================================================================
//
//  流动和闪烁
//
//==========================================================================

void FlowAndBlinn(inout half3 color, inout half alpha, half normalizeDist, half nnDist)
{
#if defined(_FLOW_AUTO)
    half cau = normalizeDist - frac(_Time.y * _ParamsFlow.x);
    cau = frac(cau * _ParamsFlow.z);
    half power = cau > 0 ? _ParamsFlow.y : 1;
    half flowStrength = pow(saturate(abs(cau) + _Progress2), power) * _ParamsFlow.w;
    color = lerp(color, lerp(_CauColor.rgb, color + _CauColor.rgb, _CauColor.aaa), flowStrength);
    alpha = max(alpha, _CauColor.a * flowStrength);

#elif defined(_FLOW_MANUAL)
    half vis = step(normalizeDist, _Progress);
    half distance = 0.2; //内圈 和 外圈渐变参数
    //内圈可见控制
    half vis1 = (0.0001<nnDist) * (nnDist<normalizeDist) *((normalizeDist - distance) < nnDist);
    //外圈可见控制
    half vis2 = step(1 - distance,normalizeDist);
    half cau = frac(normalizeDist - _Progress) * vis;
    half power = cau > 0 ? 10 : 1;
    half flowStrength = pow(saturate(abs(cau) + _Progress2 * vis ), power) + vis1*pow((distance - normalizeDist + nnDist)/distance,5) + vis2 * pow(1-(1-normalizeDist)/distance,3);
    #if defined(_SHAPE_ROUND2)
        color = lerp(color, (_Border)*(1-vis) + vis * _CauColor , flowStrength);
        alpha = max(alpha, _Color.a * flowStrength);
    #else
        color = lerp(color, lerp(_CauColor.rgb, color + _CauColor.rgb, _CauColor.aaa), flowStrength);
        alpha = max(alpha, _CauColor.a * flowStrength);
    #endif
#endif

    #ifdef _BLINN_ON
    //alpha *= saturate(sin(_Time.y * _ParamsEx.z * UNITY_TWO_PI) * _ParamsEx.w * 0.5 + 0.5);
    #endif
}

//==========================================================================
//
//  顶点与片段
//
//==========================================================================

v2f vert (float4 vertex : POSITION)
{
    v2f o;
    vertex.xyz *= 0.01;
    o.pos.xy = vertex.xz * 2;//用于坐标计算，-1~1区间

    #if defined(_SHAPE_RECT)

        half l = (o.pos.y > 0) ? _Params.x : _Params.y;
        vertex.xz *= half2(_Params.z, l * 2);

    #elif defined(_SHAPE_ROUND) || defined(_SHAPE_ROUND2)

        float2 dir;
        sincos(_ParamsEx.y * _Time.y, dir.x, dir.y); // get angle
        vertex.xz = mul(float2x2(dir.y, -dir.x, dir.x, dir.y), vertex.xz);
        vertex.xz *= _Params.xx * 2;

    #elif defined(_SHAPE_FAN)

        o.angle = radians(_Params.z * 0.5);//半角弧度
        vertex.xz *= _Params.xx * 2;

    #endif

    o.vertex = UnityObjectToClipPos(vertex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    half4 color = (half4)1;

    half inBorderRange;
    half nnDist = 1;
    #if defined(_SHAPE_RECT)

        half2 dist = i.pos;
        half2 distN = abs(dist);
        half w = _Params.z * 0.5;
        half2 l = dist.y > 0 ? _Params.xy : _Params.yx;
        half2 b = _Params.ww / half2(w, l.x);
        int2 posInRange = step(distN, 1 - b);

        UNITY_BRANCH
        if (l.y < b.y)
        {
            posInRange.y *= step(b.y, distN.y);
        }

        inBorderRange = 1 - posInRange.x * posInRange.y;

        #if defined(_Caution2)
            half nDist = distN.y;
            half fade = saturate(distN.y- _CenterOffset);
            fade = pow(fade, _Opacity * 2)*_FadeCo;
        #else
            half nDist = distN.y;
            half fade = distN.y;
            fade = pow(fade, _Opacity * 2);
        #endif

        
        color.a = fade;

        half2 uv = half2(dist.x, distN.y);
        uv.x = uv.x * _ParamsEx.x * 0.5 + 0.5;

        half4 decal = tex2D(_Decal, uv);
        color *= decal;

    #elif defined(_SHAPE_ROUND) || defined(_SHAPE_ROUND2)
        half2 InB = _Params.yw / _Params.xx;//内圈与边缘按照与外圈的比例换算
        half2 dist = length(i.pos).xx;// 当前位置单位距离
        half nDist = dist.x; //外圈
        nnDist = InB.x; //内圈

        half3 compR = half3(1, dist.x, 1 - saturate(InB.y));
        half3 compL = half3(dist.x, InB.x, dist.x);

        //x代表外圈范围，y代表外圈出去内圈的范围,z代表限制后的范围
        int3 comp = step(compL, compR);
        half posInRange = comp.x * comp.y;
        half posInBorderRange = comp.z * comp.y;
        inBorderRange = posInRange * (1 - posInBorderRange);
        clip(posInRange - 0.001);

         #if defined (_Caution2)
            half distanceTemp = saturate(dist.x - InB.x - _CenterOffset);
            half fade = distanceTemp/ (1.0f - InB.x);
            fade = pow(fade, _Opacity * 2.0f)*_FadeCo;
        #else
            half fade = (dist.x - InB.x) / (1 - InB.x);
            fade = pow(fade, _Opacity * 2);
        #endif

        color.a = fade;

        half2 uv = i.pos * _ParamsEx.x * 0.5 + 0.5;
        half4 decal = tex2D(_Decal, uv); 

        color *= decal;
    #elif defined(_SHAPE_FAN)

        half2 dist = length(i.pos).xx;// 当前位置单位距离
        half nDist = dist.x;
        half2 angle = half2(i.angle, abs(atan2(i.pos.x, i.pos.y)));//x = 最大夹角，y = 当前角度

        half2 InB = _Params.yw / _Params.xx;//内圈与边缘按照与外圈的比例换算
        half3 compR = half3(1, dist.x, angle.x);
        half3 compL = half3(dist.x, InB.x, angle.y);
        int3 comp = step(compL, compR);
        half posInRange = comp.x * comp.y * comp.z;
        half2 compRB = half2(1 - InB.y, angle.x - InB.y);
        half2 compLB = half2(dist.x, angle.y);
        int2 compB = step(compLB, compRB);
        half posInBorderRange = compB.x * compB.y;
        inBorderRange = posInRange * (1 - posInBorderRange);
        clip(posInRange - 0.001);

        #if defined(_Caution2)
            half distanceTemp = saturate(dist.x - InB.x - _CenterOffset);
            half fade = distanceTemp/ (1.0f - InB.x);
            fade = pow(fade, _Opacity * 2.0f)*_FadeCo;
        #else
            half fade = (dist.x - InB.x) / (1 - InB.x);
            fade = pow(fade, _Opacity * 2);
        #endif

        
        color.a = fade;
      
        half2 uv = half2((angle.x - angle.y) * _ParamsEx.x / angle.x, nDist);
        half4 decal = tex2D(_Decal, uv); 
        color *= decal;

    #else

        inBorderRange = 0;
        half nDist = 1e-3;

    #endif

    #if defined(_SHAPE_ROUND2)
        color *= _Color;
    #else
        color.rgb *= lerp(_Color.rgb, _Border.rgb, inBorderRange * _Border.a);
    #endif
    FlowAndBlinn(color.rgb, color.a, nDist,nnDist);

#if defined (_Caution2)
#ifdef _BASETOGGLE_ON
    half4 baseTex = tex2D(_BaseTex, uv);
    half tempAlpha = color.a;
    color.a = (baseTex * _BaseColor).a + color.a ;
    //[baseTex.a *  decal.a] base贴图和decal的交集特别处理，对该区域的a值做lerp，让交集区域颜色更平滑
    color.rgb = (baseTex * _BaseColor).rgb * baseTex.a * (1 - decal.a) + 
                baseTex.a *  decal.a * lerp(baseTex.rgb*_BaseColor,color.rgb,decal.a * baseTex.a * tempAlpha) + 
                color.rgb * (1-baseTex.a) ;
#endif
#endif

    return saturate(color);
}

#endif