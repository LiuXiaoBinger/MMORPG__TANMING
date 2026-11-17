#ifndef COLORCORRECT_INCLUDE
#define COLORCORRECT_INCLUDE

half3 RGB2HSV(half3 c) 
{	
	const half4 kh = half4(0.0, -0.33333333, 0.66666667, -1.0);

	bool pTh = c.g > c.b;
	half4 p = pTh ? half4(c.gb, kh.xy) : half4(c.bg, kh.wz);

	bool qTh = c.r > p.x;
	half4 q = qTh ? half4(c.r, p.yzx) : half4(p.xyw, c.r);
	
	half d = q.x - min(q.w, q.y);
	half e = 1.0e-6h;
	return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

half3 HSV2RGB(half3 c) 
{
	const half4 kc = half4(1.0, 0.66666667, 0.33333333, 3.0);
	half3 p = abs(frac(c.xxx + kc.xyz) * 6.0 - kc.www);
	return c.z * lerp(kc.xxx, saturate(p - kc.xxx), c.y);
}

//变色处理

half3 _HairCL;
half3 _HairCM;
half3 _HairCD;
half4 _HairCH;

half3 _EyeCL;
half3 _EyeCM;
half3 _EyeCD;
half4 _EyeCH;

inline bool4 GetLevel(half m, half4 thres)
{
	half4 level = thres < m.xxxx;
	level.wzy *= (1).xxx - level.zyx;
	return level > 0;
}

half3 ColorCorrect(half3 col, half3 lc, half3 mc, half3 dc, half4 t)
{
	half m = col.r;
	bool4 lev = GetLevel(m, t);
	half2 th2 = saturate((m.xx - t.yw) / (t.xz - t.yw));

	//>x= 亮部 >y= 次亮部 >z= 中部 >w= 次中部 0= 暗部
	half3 cc = lev.x ? lc
		: lev.y ? lerp(mc, lc, th2.xxx)
		: lev.z ? mc
		: lev.w ? lerp(dc, mc, th2.yyy)
		: dc;

	return col * cc;
}

half3 ColorCorrectEye(half3 col, half mask)
{
	half3 tint = ColorCorrect(col, _EyeCL * 1.05, _EyeCM * 1.15, _EyeCD * 1.5, _EyeCH);
	return lerp(col, tint * 1.25, mask * step(1e-3, _EyeCH.r));
}

half3 ColorCorrectHair(half3 col, half mask)
{
	half3 tint = ColorCorrect(col, _HairCL * 1.05, _HairCM * 1.15, _HairCD * 1.5, _HairCH);
	return lerp(col, tint * 1.25, mask * step(1.0e-3, _HairCH.r));
}

half3 ColorCorrectPart(half3 col, half mask, bool isHair)
{
	half4x4 cc = isHair ? half4x4(_HairCL, 0, _HairCM, 0, _HairCD, 0, _HairCH) 
		: half4x4(_EyeCL, 0, _EyeCM, 0, _EyeCD, 0, _EyeCH);

	half3 tint = ColorCorrect(col, cc[0].xyz * 1.05, cc[1].xyz * 1.15, cc[2].xyz * 1.5, cc[3]);
	return lerp(col, tint, mask * (cc[3].x > 0));
}

#endif