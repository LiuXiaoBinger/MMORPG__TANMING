using System;
using UnityEngine;
using System.Text;



/// <summary>
/// 客户端通用数学、几何与字符串工具集合，沿用 RO 的静态函数入口。
/// 包含浮点近似判等、快速哈希、角度/向量换算、线段矩形圆相交判定及递归查找节点等。
/// </summary>
public class MCommonFunctions
    {
        /// <summary>自增的临时ID计数器，NewId 每次先加 1 再返回，用于临时标识。</summary>
        private static int _newId = 0;
        /// <summary>浮点判等的最小误差阈值（约1e-6），小于该差值即视为相等。</summary>
        private static readonly float _eps = 0.000001f; // 最小判等的float值
        /// <summary>预计算的正弦查表，下标为角度*2（0.5度一步，覆盖0~360度），减少运行时三角计算。</summary>
        private static float[] _ROSin = new float[721];
        /// <summary>预计算的余弦查表，与 _ROSin 同步生成。</summary>
        private static float[] _ROCos = new float[721];

        /// <summary>浮点判等阈值的只读出口。</summary>
        public static float XEps
        {
            get { return _eps; }
        }
        /// <summary>将临时计数加 1 后返回；该计数不持久化。</summary>
        public static int NewId
        {
            get { return ++_newId; }
        }
        /// <summary>把当前系统时间的 Tick（100 纳秒单位）与自增计数相加；不是持久化的唯一编号。</summary>
        public static long UniqueToken
        {
            get { return DateTime.Now.Ticks + NewId; }
        }

        /// <summary>首次使用工具类时，按每 0.5 度建立一张正弦、余弦表，值保留六位小数。</summary>
        static MCommonFunctions()
        {
            // 当前三角函数表的下标，每项对应 0.5 度。
            int x = 0;

            // 当前表项的角度值，单位为度。
            float v = 0;
            // 度数换算为弧度的系数，供 Sin、Cos 使用。
            float d2r = 0.0174532925f;

            while (x < 721)
            {
                _ROSin[x] = Mathf.Sin(d2r * (v));
                _ROSin[x] *= 1000000;
                _ROSin[x] = Mathf.Round(_ROSin[x]) / 1000000;
                _ROCos[x] = Mathf.Cos(d2r * (v));
                _ROCos[x] *= 1000000;
                _ROCos[x] = Mathf.Round(_ROCos[x]) / 1000000;

                ++x; v += 0.5f;
            }
        }

        #region hash

        /// <summary>按字符顺序计算乘 33 哈希；str 为 null 时返回 0，计算规则与字符串重载相同。</summary>
        public static uint GetHash(StringBuilder str)
        {
            if (str == null) return 0;

            // 逐字符累加的哈希结果，从 0 开始。
            uint hash = 0;
            // i 是当前字符在输入字符串中的位置。
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 5) + hash + str[i];
            }

            return hash;
        }

        /// <summary>
        /// 逐字符计算哈希：把已有结果乘 33，再加当前字符的编码。
        /// <![CDATA[
        /// 不断的乘33 nHash = nHash*33 + *key++
        /// seed2 + (seed2 << 5) 相当于乘以33
        /// ]]>
        /// </summary>
        /// <param name="str">待计算的字符串；null 返回 0。</param>
        /// <returns>逐字符累积的 32 位无符号哈希；不同字符串仍可能得到相同值。</returns>
        public static uint GetHash(string str)
        {
            if (str == null) return 0;

            // 逐字符累加的哈希结果，从 0 开始。
            uint hash = 0;
            // i 是当前字符在输入字符串中的位置。
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 5) + hash + str[i];
            }

            return hash;
        }

        /// <summary>把 str 的每个字符转为小写后计算哈希，用于忽略大小写的比较；null 返回 0。</summary>
        public static uint GetLowerHash(string str)
        {
            if (str == null) return 0;

            // 逐字符累加的哈希结果，从 0 开始。
            uint hash = 0;
            // i 是当前字符在输入字符串中的位置。
            for (int i = 0; i < str.Length; i++)
            {
                // 当前字符的小写形式，用于后续哈希累加。
                char c = char.ToLower(str[i]);
                hash = (hash << 5) + hash + c;
            }

            return hash;
        }
        /// <summary>从 hash 继续累加 str 的字符；先转小写，把路径分隔符改成点、空格改成下划线。</summary>
        public static uint GetLowerHashRelpaceDot(uint hash, string str)
        {
            if (string.IsNullOrEmpty(str)) return hash;

            // i 是当前字符在输入字符串中的位置。
            for (int i = 0; i < str.Length; i++)
            {
                // 当前字符的小写形式，用于后续哈希累加。
                char c = char.ToLower(str[i]);
                if (c == '/' || c == '\\')
                {
                    c = '.';
                }
                else if (c == ' ')
                {
                    c = '_';
                }
                hash = (hash << 5) + hash + c;
            }

            return hash;
        }

        #endregion

        #region float

        /// <summary>判断 a 与 b 的差是否小于 0.000001，容忍浮点计算误差。</summary>
        public static bool IsEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < _eps;
        }
        /// <summary>比较双精度数 a、b，仍使用 0.000001 的绝对误差。</summary>
        public static bool IsEqual(double a, double b)
        {
            return Math.Abs(a - b) < _eps;
        }

        /// <summary>判断 a 是否在零附近，容许绝对误差小于 0.000001。</summary>
        public static bool IsEqualZero(float a)
        {
            return IsEqual(a, 0f);
        }

        /// <summary>判断双精度数 a 是否在零附近，容许绝对误差小于 0.000001。</summary>
        public static bool IsEqualZero(double a)
        {
            return IsEqual(a, 0.0);
        }

        /// <summary>精确比较两个整数 a、b，不使用浮点误差。</summary>
        public static bool IsLongDataEqual(long a, long b)
        {
            return a==b;
        }
        /// <summary>只有 a 小于 b 且差值超过近似相等范围时才返回 true。</summary>
        public static bool IsLess(float a, float b)
        {
            // 两数之差，用于先排除误差范围再判断正负。
            float v = a - b;
            return !IsEqual(v, 0) && (v < 0);
        }
        /// <summary>按双精度计算 a 与 b 的差，排除近似相等后判断 a 是否更小。</summary>
        public static bool IsLess(double a, double b)
        {
            // 双精度差值，用于先排除误差范围再判断正负。
            double v = a - b;
            return !IsEqual(v, 0) && (v < 0);
        }
        /// <summary>只有 a 大于 b 且不属于近似相等时才返回 true。</summary>
        public static bool IsGreater(float a, float b)
        {
            // 两数之差，用于先排除误差范围再判断正负。
            float v = a - b;
            return !IsEqual(v, 0) && (v > 0);
        }
        /// <summary>按双精度计算 a 与 b 的差，排除近似相等后判断 a 是否更大。</summary>
        public static bool IsGreater(double a, double b)
        {
            // 双精度差值，用于先排除误差范围再判断正负。
            double v = a - b;
            return !IsEqual(v, 0) && (v > 0);
        }
        /// <summary>判断 a 是否近似等于 b，或明确小于 b。</summary>
        public static bool IsEqualLess(float a, float b)
        {
            return IsEqual(a, b) || IsLess(a, b);
        }
        /// <summary>使用同一绝对误差判断双精度数 a 是否小于或近似等于 b。</summary>
        public static bool IsEqualLess(double a, double b)
        {
            return IsEqual(a, b) || IsLess(a, b);
        }
        /// <summary>判断 a 是否近似等于 b，或明确大于 b。</summary>
        public static bool IsEqualGreater(float a, float b)
        {
            return IsEqual(a, b) || IsGreater(a, b);
        }
        /// <summary>使用同一绝对误差判断双精度数 a 是否大于或近似等于 b。</summary>
        public static bool IsEqualGreater(double a, double b)
        {
            return IsEqual(a, b) || IsGreater(a, b);
        }
        /// <summary>检查 n 的小数部分是否接近 0 或 1，也将接近下一个整数的值视作整数。</summary>
        public static bool IsInteger(float n)
        {
            return Mathf.Abs(n - (int)n) < _eps || Mathf.Abs(n - (int)n) > (1 - _eps);
        }
        /// <summary>检查双精度数 n 距相邻整数是否在允许误差内；整数部分通过 int 转换取得。</summary>
        public static bool IsInteger(double n)
        {
            return Math.Abs(n - (int)n) < _eps || Math.Abs(n - (int)n) > (1 - _eps);
        }

        /// <summary>将 value 限制在包含端点的 min、max 范围内。</summary>
        public static int Range(int value, int min, int max)
        {
            value = Math.Max(value, min);
            return Math.Min(value, max);
        }

        /// <summary>尝试把 obj 拆箱为 uint；成功时输出该值并返回 true，失败时输出 0。这里不解析字符串。</summary>
        public static bool TryParseUint(object obj, out uint retUint)
        {
            // 是否成功将输入对象拆箱为 uint。
            bool ret = false;
            try
            {
                retUint = (uint)obj;
                ret = true;
            }
            catch (Exception)
            {
                retUint = 0;
            }

            return ret;
        }

        #endregion

        #region geometry

        /// <summary>
        /// 在 XZ 平面判断以原点为中心的矩形与圆是否重叠；恰好相切时返回 false。
        /// </summary>
        /// <param name="rectw">矩形沿 X 轴的半宽。</param>
        /// <param name="recth">矩形沿 Z 轴的半长。</param>
        /// <param name="center">圆心相对矩形中心的位置，只使用 X、Z。</param>
        /// <param name="radius">圆半径</param>
        /// <returns>圆心到矩形最近点的距离严格小于圆半径时为 true。</returns>
        public static bool IsRectCycleCross(float rectw, float recth, Vector3 center, float radius)
        {
            // 圆心到矩形最近点的 XZ 偏移，圆心在矩形内时为零。
            Vector3 u = new Vector3(Mathf.Max(Mathf.Abs(center.x) - rectw, 0.0f), 0, Mathf.Max(Mathf.Abs(center.z) - recth, 0.0f));
            return u.sqrMagnitude < radius * radius;
        }

        /*
         * 根据 begin 到 end 的方向检测圆；t 接收从起点到近侧交点的计算距离。
         * 当前实现不检查交点是否超过 end，且角度直接传给 Cos；这里保留原算法，
         * 不能据此认定它已正确实现有限线段与圆相交。
         */

        public static bool Intersection(Vector2 begin, Vector2 end, Vector2 center, float radius, out float t)
        {
            t = 0;

            // 圆半径的平方，后面与平方距离比较。
            float sqrtR = radius * radius;

            // 起点指向圆心的向量。
            Vector2 a = center - begin;
            // 起点与圆心之间的平方距离。
            float cdist = a.sqrMagnitude;

            if (cdist < sqrtR)
            {
                // 起点已在圆内，距离 t 保持为 0。
                return true;
            }
            else
            {
                // 起点指向终点的方向，本实现未用其长度限制交点。
                Vector2 d = end - begin;

                if (d.sqrMagnitude > 0)
                {
                    // 原算法计算的方向投影；Angle 返回度数，而 Cos 接收弧度，保留此现有差异。
                    float project = Mathf.Sqrt(cdist) * Mathf.Cos(Vector2.Angle(a, d));

                    if (project >= 0)
                    {
                        // 按投影结果求出的垂直距离平方。
                        float m = cdist - project * project;
                        if (m < sqrtR)
                        {
                            // 半径平方减去垂距平方，用于算近侧交点距离。
                            float q = sqrtR - m;
                            t = project - Mathf.Sqrt(q);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>计算两个 XZ 向量的二维叉积；x1、z1 与 x2、z2 分别为两个向量的分量。</summary>
        private static float CrossProduct(float x1, float z1, float x2, float z2)
        {
            return x1 * z2 - x2 * z1;
        }

        /// <summary>
        /// 在 XZ 平面判断两条线段是否相交，端点接触和共线重叠也算相交。
        /// </summary>
        /// <param name="p1">第一条线段的起点。</param>
        /// <param name="p2">第一条线段的终点。</param>
        /// <param name="q1">第二条线段的起点。</param>
        /// <param name="q2">第二条线段的终点。</param>
        /// <returns>两条线段在 XZ 平面相交或接触时为 true。</returns>
        public static bool IsLineSegmentCross(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            // 先比较两条线段的包围范围，范围不重叠就不用继续算叉积。
            // X 轴投影区间相交：两条线段在水平 X 方向上存在重叠范围。
            bool xOverlap = Mathf.Min(p1.x, p2.x) <= Mathf.Max(q1.x, q2.x) &&
                Mathf.Min(q1.x, q2.x) <= Mathf.Max(p1.x, p2.x);
            // Z 轴投影区间相交：两条线段在纵深 Z 方向上存在重叠范围。
            bool zOverlap = Mathf.Min(p1.z, p2.z) <= Mathf.Max(q1.z, q2.z) &&
                Mathf.Min(q1.z, q2.z) <= Mathf.Max(p1.z, p2.z);
            if (xOverlap && zOverlap)
            {
                // 第一条线段起点相对第二条线段的有向面积，用符号判断位于哪一侧。
                float p1xq = CrossProduct(p1.x - q1.x, p1.z - q1.z,
                                           q2.x - q1.x, q2.z - q1.z);
                // 第一条线段终点相对第二条线段的有向面积。
                float p2xq = CrossProduct(p2.x - q1.x, p2.z - q1.z,
                                           q2.x - q1.x, q2.z - q1.z);

                // 第二条线段起点相对第一条线段的有向面积。
                float q1xp = CrossProduct(q1.x - p1.x, q1.z - p1.z,
                                           p2.x - p1.x, p2.z - p1.z);
                // 第二条线段终点相对第一条线段的有向面积；两组乘积都非正才相交。
                float q2xp = CrossProduct(q2.x - p1.x, q2.z - p1.z,
                                           p2.x - p1.x, p2.z - p1.z);

                return ((p1xq * p2xq <= 0) && (q1xp * q2xp <= 0));
            }

            return false;
        }
        /// <summary>把向量 v 的 Y 分量清零，保留 XZ 分量和原有水平长度。</summary>
        public static Vector3 Horizontal(Vector3 v)
        {
            v.y = 0;
            return v;
        }
        /// <summary>先去掉 v 的高度分量，再返回水平方向的单位向量。</summary>
        public static Vector3 HorizontalNormal(Vector3 v)
        {
            v.y = 0;
            return v.normalized;
        }
        /// <summary>将 degree 改写成最接近 basic 的等效角度，沿两者间较短的旋转方向计算；单位为度。</summary>
        public static float AngleNormalize(float basic, float degree)
        {
            // 基准角度对应的水平朝向。
            Vector3 v1 = FloatToAngle(basic);
            // 待换算角度对应的水平朝向。
            Vector3 v2 = FloatToAngle(degree);

            // 两个朝向之间 0 到 180 度的最小夹角。
            float angle = Vector3.Angle(v1, v2);

            return Clockwise(v1, v2) ? basic + angle : basic - angle;
        }

        /// <summary>在二维平面把 v 顺时针旋转 degree 度；normalized 为 true 时返回单位向量。</summary>
        public static Vector2 HorizontalRotateVetor2(Vector2 v, float degree, bool normalized = true)
        {
            degree = -degree;

            // 旋转角度换成弧度。
            float rad = degree * Mathf.Deg2Rad;
            // 本次旋转的正弦值。
            float sinA = Mathf.Sin(rad);
            // 本次旋转的余弦值。
            float cosA = Mathf.Cos(rad);

            // 旋转后的二维 X 分量，先暂存避免覆盖旧值。
            float x = v.x * cosA - v.y * sinA;
            // 旋转后的二维 Y 分量。
            float y = v.x * sinA + v.y * cosA;

            v.x = x;
            v.y = y;
            return normalized ? v.normalized : v;
        }

        /// <summary>在 XZ 平面旋转 v，degree 先取负再按整度查表；normalized 控制是否归一化。调用方须给出查表支持的角度范围。</summary>
        public static Vector3 HorizontalRotateVetor3(Vector3 v, float degree, bool normalized = true)
        {
            degree = -degree;
            // 把取反后的角度四舍五入为整数度，随后转换成表下标。
            int deg = Mathf.FloorToInt(degree + 0.5f);
            if (deg < 0) deg += 360;
            // 表每 0.5 度一项，角度乘 2 后读取正弦。
            float sinA = _ROSin[deg << 1];
            // 同一角度对应的余弦查表值。
            float cosA = _ROCos[deg << 1];

            // 旋转后的 X 分量，Y 高度不参与水平旋转。
            float x = v.x * cosA - v.z * sinA;
            // 旋转后的 Z 分量。
            float z = v.x * sinA + v.z * cosA;

            v.x = x;
            v.z = z;
            return normalized ? v.normalized : v;
        }

        /// <summary>将 100 纳秒单位的 tick 换算为秒，先整除为毫秒，所以不足 1 毫秒的部分会丢弃。</summary>
        public static float TicksToSeconds(long tick)
        {
            // Tick 截取为整数毫秒。
            long ms = tick / 10000;
            return ms / 1000.0f;
        }

        /// <summary>将 time 秒换算为 100 纳秒单位的 Tick；先截取整数毫秒。</summary>
        public static long SecondsToTicks(float time)
        {
            // 秒数截取为整数毫秒。
            long ms = (long)(time * 1000.0f);
            return ms * 10000;
        }

        /// <summary>求正前方到 dir 的有符号角度，顺时针为正、逆时针为负，单位为度。</summary>
        public static float AngleToFloat(Vector3 dir)
        {
            // 正前方与目标方向的无符号夹角，范围为 0 到 180 度。
            float face = Vector3.Angle(Vector3.forward, dir);
            return Clockwise(Vector3.forward, dir) ? face : -face;
        }

        /// <summary>求 from 到 to 的夹角，再按 XZ 平面的顺逆时针给出正负号；单位为度。</summary>
        public static float AngleWithSign(Vector3 from, Vector3 to)
        {
            // 两个方向的无符号夹角，下一行决定正负。
            float angle = Vector3.Angle(from, to);
            return Clockwise(from, to) ? angle : -angle;
        }

        /// <summary>以正 Z 方向为起点，绕 Y 轴旋转 angle 度，返回对应朝向。</summary>
        public static Vector3 FloatToAngle(float angle)
        {
            return Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }

        /// <summary>根据方向 v 相对正前方的角度，生成仅绕 Y 轴旋转的四元数。</summary>
        public static Quaternion VectorToQuaternion(Vector3 v)
        {
            return FloatToQuaternion(AngleWithSign(Vector3.forward, v));
        }

        /// <summary>生成绕 Y 轴旋转 angle 度的四元数，俯仰和翻滚角均为 0。</summary>
        public static Quaternion FloatToQuaternion(float angle)
        {
            return Quaternion.Euler(0, angle, 0);
        }

        /// <summary>从 pos 上方向下找地面，把 forward 调整到地面切线方向；未命中时返回单位旋转。</summary>
        public static Quaternion RotateToGround(Vector3 pos, Vector3 forward)
        {
            // 从位置上方 1 个单位向下探测 5 个单位，读取地面的法线。
            // 向下射线命中的位置与地面法线。
            RaycastHit hit;

            if (Physics.Raycast(new Ray(pos + Vector3.up, Vector3.down), out hit, 5.0f, 1 | 1 << 9))
            {
                // v1 为地面向上方向，v2 为待修正的前方；正交化后两者垂直。
                Vector3 v1 = hit.normal; Vector3 v2 = forward;
                Vector3.OrthoNormalize(ref v1, ref v2);
                return Quaternion.LookRotation(v2, v1);
            }

            return Quaternion.identity;
        }

        /*
         * 用叉积符号判断 relativity 是否位于 fiduciary 的顺时针方向（XZ 平面）。
         */
        public static bool Clockwise(Vector3 fiduciary, Vector3 relativity)
        {
            // XZ 平面叉积的符号，正数表示顺时针一侧。
            float r = fiduciary.z * relativity.x - fiduciary.x * relativity.z;
            return r > 0;
        }

        /// <summary>通过二维叉积判断 relativity 是否位于基准向量 fiduciary 的顺时针一侧；共线返回 false。</summary>
        public static bool Clockwise(Vector2 fiduciary, Vector2 relativity)
        {
            // 二维叉积的符号，正数表示顺时针一侧。
            float r = fiduciary.y * relativity.x - fiduciary.x * relativity.y;
            return r > 0;
        }

        /*
         * 将世界点转到矩形自身坐标，再比较 X、Z 与半边长。
         * 不检查高度，边界上的点不算在内部。
         */
        public static bool IsInRect(Vector3 point, Vector3 halfScale, Vector3 center, Quaternion rotation)
        {
            //float y = -(rotation.eulerAngles.y % 360) / 180.0f * Mathf.PI;
            /*
             * Quaternion = (xi + yj + zk + w ) = (x, y, z, w)
             * Q = cos (a/2) + i (x * sin(a/2)) + j (y * sin(a/2)) + k (z * sin(a/2))    (a 为旋转角度)
             * Q.w = cos (angle / 2) 
             * Q.x = axis.x * sin (angle / 2)
             * Q.y = axis.y * sin (angle / 2)
             * Q.z = axis.z * sin (angle / 2)
             */
            //Quaternion q = Quaternion.identity;
            //q.w = Mathf.Cos(y / 2.0f);
            //q.x = 0;
            //q.y = Mathf.Sin(y / 2.0f);
            //q.z = 0;

            point = Quaternion.Inverse(rotation) * (point - center);
            //return (point.x > rect[0].x && point.x < rect[3].x && point.z > rect[0].z && point.z < rect[2].z);
            // 落点在本地水平 X 半轴范围内。
            bool insideX = point.x > -halfScale.x && point.x < halfScale.x;
            // 落点在本地纵深 Z 半轴范围内。
            bool insideZ = point.z > -halfScale.z && point.z < halfScale.z;
            return insideX && insideZ;
        }

        /// <summary>将世界点 point 转入以 center、rotation 定义的盒子坐标，比较三个方向的半尺寸 halfScale；不含边界。</summary>
        public static bool IsInRect3D(Vector3 point, Vector3 center, Vector3 halfScale, Quaternion rotation)
        {
            //float y = -(rotation.eulerAngles.y % 360) / 180.0f * Mathf.PI;
            /*
             * Quaternion = (xi + yj + zk + w ) = (x, y, z, w)
             * Q = cos (a/2) + i (x * sin(a/2)) + j (y * sin(a/2)) + k (z * sin(a/2))    (a 为旋转角度)
             * Q.w = cos (angle / 2) 
             * Q.x = axis.x * sin (angle / 2)
             * Q.y = axis.y * sin (angle / 2)
             * Q.z = axis.z * sin (angle / 2)
             */
            /*Quaternion q = Quaternion.identity;
            q.w = Mathf.Cos(y / 2.0f);
            q.x = 0;
            q.y = Mathf.Sin(y / 2.0f);
            q.z = 0;
            var point_enter = point;
            var point1 = q * (point - center);
            var qq = Quaternion.Inverse(rotation);*/
            point = Quaternion.Inverse(rotation) * (point - center);
            // 落点在本地水平 X 半轴范围内。
            bool insideX = point.x > -halfScale.x && point.x < halfScale.x;
            // 落点在本地竖直 Y 半轴范围内。
            bool insideY = point.y > -halfScale.y && point.y < halfScale.y;
            // 落点在本地纵深 Z 半轴范围内。
            bool insideZ = point.z > -halfScale.z && point.z < halfScale.z;
            return insideX && insideY && insideZ;
        }

        /// <summary>把 point 转入矩形坐标后比较 XZ 范围；rect 的 y 对应 Z，矩形边界也算内部。</summary>
        public static bool IsInRectIncludeBorder(Vector3 point, Rect rect, Vector3 center, Quaternion rotation)
        {
            //float y = -(rotation.eulerAngles.y % 360) / 180.0f * Mathf.PI;

            //Quaternion q = Quaternion.identity;
            //q.w = Mathf.Cos(y / 2.0f);
            //q.x = 0;
            //q.y = Mathf.Sin(y / 2.0f);
            //q.z = 0;

            point = Quaternion.Inverse(rotation) * (point - center);
            // 落点在本地水平 X 半轴范围内。
            bool insideX = point.x >= rect.xMin && point.x <= rect.xMax;
            // 落点在本地纵深 Z 半轴范围内（rect.yMin/yMax 对应投影后的 Z 上下界）。
            bool insideZ = point.z >= rect.yMin && point.z <= rect.yMax;
            return insideX && insideZ;
        }

        /// <summary>在 XZ 平面按 center、rotation 和完整轴长 scale 检查椭圆内部；恰在边界上返回 false。</summary>
        public static bool IsInEllipse(Vector3 point, Vector3 scale, Vector3 center, Quaternion rotation)
        {
            // 椭圆中心在 X 轴的位置。
            float x0 = center.x;
            // 椭圆中心在 Z 轴的位置。
            float z0 = center.z;
            // 椭圆沿 X 方向的完整轴长，下面的乘 4 用于换算半轴。
            float a = scale.x;
            // 椭圆沿 Z 方向的完整轴长。
            float b = scale.z;

            // 绕 Y 轴的角度换算为弧度，用来旋转水平坐标。
            float y = (rotation.eulerAngles.y % 360) / 180.0f * Mathf.PI;
            // 椭圆水平旋转角的正弦值。
            float sinA = Mathf.Sin(y);
            // 椭圆水平旋转角的余弦值。
            float cosA = Mathf.Cos(y);

            // 将点相对椭圆中心的偏移按配置角度旋转，再代入椭圆方程。
            // 待测点相对椭圆中心的 X 偏移。
            float tx = point.x - x0;
            // 待测点相对椭圆中心的 Z 偏移。
            float tz = point.z - z0;
            float dx = tx * cosA - tz * sinA;  // 旋转后的 X 分量。
            float dz = tx * sinA + tz * cosA;  // 旋转后的 Z 分量。

            // 归一到椭圆半轴后的平方和，小于 1 表示在内部。
            float d = (dx * dx / a / a + dz * dz / b / b) * 4;
            return (d - 1) < 0;
        }

        /// <summary>比较 point 与 center 的水平距离，scale.x 表示圆的直径；包含边界，rotation 未参与计算。</summary>
        public static bool IsInCircle(Vector3 point, Vector3 scale, Vector3 center, Quaternion rotation)
        {
            return 2 * Vector3.Distance(Horizontal(point), Horizontal(center)) <= scale.x;
        }

        #endregion

        #region unity
        // 先检查当前节点，再按子节点顺序递归；重名时返回首先找到的那个。
        public static Transform FindChildRecursively(Transform t, string name)
        {
            if (t.name == name)
                return t;

            // i 为当前节点下的子节点序号。
            for (int i = 0; i < t.childCount; i++)
            {
                // 在当前子树里找到的同名节点，没找到则为 null。
                Transform ct = FindChildRecursively(t.GetChild(i), name);
                if (ct != null)
                    return ct;
            }
            return null;
        }

        /// <summary>先递归修改 t 的所有子节点，再将 t 自身设置为 layer 指定的 Unity 层。</summary>
        public static void SetLayerRecursive(Transform t, int layer)
        {
            // 开始遍历时的直接子节点数量。
            int childCount = t.childCount;
            // i 为要递归处理的直接子节点序号。
            for (int i = 0; i < childCount; i++)
            {
                // 当前子节点；递归会同时修改它的所有后代。
                Transform c = t.GetChild(i);
                SetLayerRecursive(c, layer);
            }
            t.gameObject.layer = layer;
        }

        #endregion

        #region string
        
        /// <summary>按完整分隔字符串 separatorStr 拆分 str，保留空段；输入字符串为空时返回 null。</summary>
        public static string[] StringSplit(string str, string separatorStr)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return str.Split(new string[] { separatorStr }, 0);
        }
        #endregion

        /*/// <summary>
        /// 停用的主机名解析示例：优先取 IPv4，没有时保留找到的 IPv6 地址。
        /// </summary>
        /// <param name="hostName">主机名或已有 IP 地址文本。</param>
        /// <returns>找到的 IP 地址；解析失败且没有候选时为空。</returns>
        public static IPAddress GetIpAddrByHostName(string hostName)
        {
            IPAddress ip = null;
            try
            {
                if (!IPAddress.TryParse(hostName, out ip))
                {
                    // 优先使用ipv4地址
                    var addresses = System.Net.Dns.GetHostAddresses(hostName);
                    foreach (var addr in addresses)
                    {
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return addr;
                        }
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            ip = addr;
                        }
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
               //MDebug.singleton.AddErrorLog("GetHostEntry exception:" + ex.Message);
            }

            return ip;
        }

        /// <summary>
        /// 停用的域名解析示例：先读取设备桥接返回的 HTTP DNS 结果，失败时再尝试系统 DNS。
        /// </summary>
        /// <param name="domain">需要查询的域名。</param>
        /// <returns>解析出的地址；HTTP DNS 与系统 DNS 均失败时返回原域名。</returns>
        public static string GetIpByHttpDns(string domain)
        {
            var device = MInterfaceMgr.singleton.GetInterface<IDevice>("MCommonLib2SDKLibBridge");
            string ipStr = device.GetHttpDns(domain);
            if (ipStr != "0_0")
            {
                var ipArr = ipStr.Split(new char[] { '_' });
                if (ipArr.Length >= 2 && ipArr[1] != "0")
                {
                    return $"[{ipArr[1]}]";
                }
                else
                {
                    return ipArr[0];
                }
            }
            else
            {
                var ipAddr = GetIpAddrByHostName(domain);
                if (ipAddr != null)
                {
                    return ipAddr.ToString();
                }
                return domain;
            }
        }

        public static string EncryptWithMD5(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            var strbul = SharedStringBuilder.Get();
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x2")); //加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位

            }
            return strbul.ToString();
        }

        public static void CopyToClipboard(string copyValue)
        {
            var device = MInterfaceMgr.singleton.GetInterface<IDevice>("MCommonLib2SDKLibBridge");
            JsonData data = new JsonData
            {
                ["value"] = copyValue
            };
            device.CopyToClipboard(data.ToJson());
        }

        public static string GetUtcNowTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        #region 冲突串

        [NoToLua]
        public static string AddConflictFlag(string key, int count = 1)
        {
            StringBuilder sb = SharedStringBuilder.Get().Append("@")
                .Append(key);
            for (int i = 0; i < count; i++)
            {
                sb.Append("#");
            }
            return sb.Append("@").ToString();
        }
        [NoToLua]
        public static string ConvertConflictKey(Dictionary<uint, string> keyDict, string key)
        {
            string originKey = key;
            if (HasConflictFlag(originKey))
            {
                originKey = GetConflictOriginKey(originKey);
            }
            key = AddConflictFlag(originKey);
            uint hash = GetHash(key);
            int count = 1;
            while (keyDict.ContainsKey(hash))
            {
                count++;
                key = AddConflictFlag(originKey, count);
                hash = GetHash(key);
            }

            return key;
        }

        private static readonly Regex _conflictReplaceRegex = new Regex(@"^@([\s\S]*?)#*@$");

        [NoToLua]
        public static string GetConflictOriginKey(string originKey)
        {
            return _conflictReplaceRegex.Replace(originKey, "$1");
        }
        [NoToLua]
        public static bool HasConflictFlag(string key)
        {
            return key.StartsWith("@") && key.EndsWith("@");
        }*/

        //#endregion

    }
