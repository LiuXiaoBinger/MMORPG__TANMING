using System;
using UnityEngine;
using System.Text;



/// <summary>
/// 客户端通用数学、几何与字符串工具集合，沿用 RO 的静态函数入口。
/// 包含浮点近似判等、快速哈希、角度/向量换算、线段矩形圆相交判定及递归查找节点等。
/// </summary>
public class MCommonFunctions
    {
        /// <summary>自增的临时ID计数器，NewId 每次取值后自增，用于生成进程内唯一临时标识。</summary>
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
        /// <summary>获取一个自增的进程内唯一临时ID。</summary>
        public static int NewId
        {
            get { return ++_newId; }
        }
        /// <summary>由系统 Tick 毫秒与自增ID组合生成的唯一令牌，避免同毫秒碰撞。</summary>
        public static long UniqueToken
        {
            get { return DateTime.Now.Ticks + NewId; }
        }

        static MCommonFunctions()
        {
            int x = 0;

            float v = 0;
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

        public static uint GetHash(StringBuilder str)
        {
            if (str == null) return 0;

            uint hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 5) + hash + str[i];
            }

            return hash;
        }

        /// <summary>
        /// times33算法（快速哈希算法）
        /// <![CDATA[
        /// 不断的乘33 nHash = nHash*33 + *key++
        /// seed2 + (seed2 << 5) 相当于乘以33
        /// ]]>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint GetHash(string str)
        {
            if (str == null) return 0;

            uint hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash << 5) + hash + str[i];
            }

            return hash;
        }

        public static uint GetLowerHash(string str)
        {
            if (str == null) return 0;

            uint hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char c = char.ToLower(str[i]);
                hash = (hash << 5) + hash + c;
            }

            return hash;
        }
        public static uint GetLowerHashRelpaceDot(uint hash, string str)
        {
            if (string.IsNullOrEmpty(str)) return hash;

            for (int i = 0; i < str.Length; i++)
            {
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

        public static bool IsEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < _eps;
        }
        public static bool IsEqual(double a, double b)
        {
            return Math.Abs(a - b) < _eps;
        }

        public static bool IsEqualZero(float a)
        {
            return IsEqual(a, 0f);
        }

        public static bool IsEqualZero(double a)
        {
            return IsEqual(a, 0.0);
        }

        public static bool IsLongDataEqual(long a, long b)
        {
            return a==b;
        }
        public static bool IsLess(float a, float b)
        {
            float v = a - b;
            return !IsEqual(v, 0) && (v < 0);
        }
        public static bool IsLess(double a, double b)
        {
            double v = a - b;
            return !IsEqual(v, 0) && (v < 0);
        }
        public static bool IsGreater(float a, float b)
        {
            float v = a - b;
            return !IsEqual(v, 0) && (v > 0);
        }
        public static bool IsGreater(double a, double b)
        {
            double v = a - b;
            return !IsEqual(v, 0) && (v > 0);
        }
        public static bool IsEqualLess(float a, float b)
        {
            return IsEqual(a, b) || IsLess(a, b);
        }
        public static bool IsEqualLess(double a, double b)
        {
            return IsEqual(a, b) || IsLess(a, b);
        }
        public static bool IsEqualGreater(float a, float b)
        {
            return IsEqual(a, b) || IsGreater(a, b);
        }
        public static bool IsEqualGreater(double a, double b)
        {
            return IsEqual(a, b) || IsGreater(a, b);
        }
        public static bool IsInteger(float n)
        {
            return Mathf.Abs(n - (int)n) < _eps || Mathf.Abs(n - (int)n) > (1 - _eps);
        }
        public static bool IsInteger(double n)
        {
            return Math.Abs(n - (int)n) < _eps || Math.Abs(n - (int)n) > (1 - _eps);
        }

        public static int Range(int value, int min, int max)
        {
            value = Math.Max(value, min);
            return Math.Min(value, max);
        }

        public static bool TryParseUint(object obj, out uint retUint)
        {
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
        /// 判断矩形与圆是否有交叉
        /// </summary>
        /// <param name="rectw">矩形宽</param>
        /// <param name="recth">矩形高</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">圆半径</param>
        /// <returns></returns>
        public static bool IsRectCycleCross(float rectw, float recth, Vector3 center, float radius)
        {
            Vector3 u = new Vector3(Mathf.Max(Mathf.Abs(center.x) - rectw, 0.0f), 0, Mathf.Max(Mathf.Abs(center.z) - recth, 0.0f));
            return u.sqrMagnitude < radius * radius;
        }

        /*
         * check whether the specific circle is intersected by the line.
         * the first intersection point from begin is returned by value t,
         * which means the distance from begin-point.
         */

        public static bool Intersection(Vector2 begin, Vector2 end, Vector2 center, float radius, out float t)
        {
            t = 0;

            float sqrtR = radius * radius;

            Vector2 a = center - begin;
            float cdist = a.sqrMagnitude;

            if (cdist < sqrtR)
            {
                //inside circle
                return true;
            }
            else
            {
                Vector2 d = end - begin;

                if (d.sqrMagnitude > 0)
                {
                    float project = Mathf.Sqrt(cdist) * Mathf.Cos(Vector2.Angle(a, d));

                    if (project >= 0)
                    {
                        float m = cdist - project * project;
                        if (m < sqrtR)
                        {
                            float q = sqrtR - m;
                            t = project - Mathf.Sqrt(q);

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static float CrossProduct(float x1, float z1, float x2, float z2)
        {
            return x1 * z2 - x2 * z1;
        }

        /// <summary>
        /// 线段是否交叉
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool IsLineSegmentCross(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            //fast detect
            // X 轴投影区间相交：两条线段在水平 X 方向上存在重叠范围。
            bool xOverlap = Mathf.Min(p1.x, p2.x) <= Mathf.Max(q1.x, q2.x) &&
                Mathf.Min(q1.x, q2.x) <= Mathf.Max(p1.x, p2.x);
            // Z 轴投影区间相交：两条线段在纵深 Z 方向上存在重叠范围。
            bool zOverlap = Mathf.Min(p1.z, p2.z) <= Mathf.Max(q1.z, q2.z) &&
                Mathf.Min(q1.z, q2.z) <= Mathf.Max(p1.z, p2.z);
            if (xOverlap && zOverlap)
            {
                //( p1 - q1 ) * ( q2 - q1 )
                float p1xq = CrossProduct(p1.x - q1.x, p1.z - q1.z,
                                           q2.x - q1.x, q2.z - q1.z);
                //( p2 - q1 ) * ( q2 - q1 )
                float p2xq = CrossProduct(p2.x - q1.x, p2.z - q1.z,
                                           q2.x - q1.x, q2.z - q1.z);

                //( q1 - p1 ) * ( p2 - p1 )
                float q1xp = CrossProduct(q1.x - p1.x, q1.z - p1.z,
                                           p2.x - p1.x, p2.z - p1.z);
                //( q2 - p1 ) * ( p2 - p1 )
                float q2xp = CrossProduct(q2.x - p1.x, q2.z - p1.z,
                                           p2.x - p1.x, p2.z - p1.z);

                return ((p1xq * p2xq <= 0) && (q1xp * q2xp <= 0));
            }

            return false;
        }
        public static Vector3 Horizontal(Vector3 v)
        {
            v.y = 0;
            return v;
        }
        public static Vector3 HorizontalNormal(Vector3 v)
        {
            v.y = 0;
            return v.normalized;
        }
        public static float AngleNormalize(float basic, float degree)
        {
            Vector3 v1 = FloatToAngle(basic);
            Vector3 v2 = FloatToAngle(degree);

            float angle = Vector3.Angle(v1, v2);

            return Clockwise(v1, v2) ? basic + angle : basic - angle;
        }

        public static Vector2 HorizontalRotateVetor2(Vector2 v, float degree, bool normalized = true)
        {
            degree = -degree;

            float rad = degree * Mathf.Deg2Rad;
            float sinA = Mathf.Sin(rad);
            float cosA = Mathf.Cos(rad);

            float x = v.x * cosA - v.y * sinA;
            float y = v.x * sinA + v.y * cosA;

            v.x = x;
            v.y = y;
            return normalized ? v.normalized : v;
        }

        public static Vector3 HorizontalRotateVetor3(Vector3 v, float degree, bool normalized = true)
        {
            degree = -degree;
            int deg = Mathf.FloorToInt(degree + 0.5f);
            if (deg < 0) deg += 360;
            float sinA = _ROSin[deg << 1];
            float cosA = _ROCos[deg << 1];

            float x = v.x * cosA - v.z * sinA;
            float z = v.x * sinA + v.z * cosA;

            v.x = x;
            v.z = z;
            return normalized ? v.normalized : v;
        }

        public static float TicksToSeconds(long tick)
        {
            long ms = tick / 10000;
            return ms / 1000.0f;
        }

        public static long SecondsToTicks(float time)
        {
            long ms = (long)(time * 1000.0f);
            return ms * 10000;
        }

        public static float AngleToFloat(Vector3 dir)
        {
            float face = Vector3.Angle(Vector3.forward, dir);
            return Clockwise(Vector3.forward, dir) ? face : -face;
        }

        public static float AngleWithSign(Vector3 from, Vector3 to)
        {
            float angle = Vector3.Angle(from, to);
            return Clockwise(from, to) ? angle : -angle;
        }

        public static Vector3 FloatToAngle(float angle)
        {
            return Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }

        public static Quaternion VectorToQuaternion(Vector3 v)
        {
            return FloatToQuaternion(AngleWithSign(Vector3.forward, v));
        }

        public static Quaternion FloatToQuaternion(float angle)
        {
            return Quaternion.Euler(0, angle, 0);
        }

        public static Quaternion RotateToGround(Vector3 pos, Vector3 forward)
        {
            //get normal
            RaycastHit hit;

            if (Physics.Raycast(new Ray(pos + Vector3.up, Vector3.down), out hit, 5.0f, 1 | 1 << 9))
            {
                Vector3 v1 = hit.normal; Vector3 v2 = forward;
                Vector3.OrthoNormalize(ref v1, ref v2);
                return Quaternion.LookRotation(v2, v1);
            }

            return Quaternion.identity;
        }

        /*
         * whether vec-r is in the clockwise direction of vec-f in x-z plane.
         */
        public static bool Clockwise(Vector3 fiduciary, Vector3 relativity)
        {
            float r = fiduciary.z * relativity.x - fiduciary.x * relativity.z;
            return r > 0;
        }

        public static bool Clockwise(Vector2 fiduciary, Vector2 relativity)
        {
            float r = fiduciary.y * relativity.x - fiduciary.x * relativity.y;
            return r > 0;
        }

        /*
         * rect point sequence:
         * left-bottom-->left-top-->right-top-->right-bottom
         * 
         * the center of rect is the (0, 0) for axis
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

        public static bool IsInEllipse(Vector3 point, Vector3 scale, Vector3 center, Quaternion rotation)
        {
            float x0 = center.x;
            float z0 = center.z;
            float a = scale.x;
            float b = scale.z;

            float y = (rotation.eulerAngles.y % 360) / 180.0f * Mathf.PI;
            float sinA = Mathf.Sin(y);
            float cosA = Mathf.Cos(y);

            //坐标旋转
            float tx = point.x - x0;
            float tz = point.z - z0;
            float dx = tx * cosA - tz * sinA;  // +x0 - x0
            float dz = tx * sinA + tz * cosA;  // +z0 - z0

            float d = (dx * dx / a / a + dz * dz / b / b) * 4;
            return (d - 1) < 0;
        }

        public static bool IsInCircle(Vector3 point, Vector3 scale, Vector3 center, Quaternion rotation)
        {
            return 2 * Vector3.Distance(Horizontal(point), Horizontal(center)) <= scale.x;
        }

        #endregion

        #region unity
        // note: if two child has same name, just return first
        public static Transform FindChildRecursively(Transform t, string name)
        {
            if (t.name == name)
                return t;

            for (int i = 0; i < t.childCount; i++)
            {
                Transform ct = FindChildRecursively(t.GetChild(i), name);
                if (ct != null)
                    return ct;
            }
            return null;
        }

        public static void SetLayerRecursive(Transform t, int layer)
        {
            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform c = t.GetChild(i);
                SetLayerRecursive(c, layer);
            }
            t.gameObject.layer = layer;
        }

        #endregion

        #region string
        
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
        /// 根据hostname获取ip信息(用于socket以及ipv6处理)
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
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
        /// 域名解析(用于解决域名劫持)
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
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
