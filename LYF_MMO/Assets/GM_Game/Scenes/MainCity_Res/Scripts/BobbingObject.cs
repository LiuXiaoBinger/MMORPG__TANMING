using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 让物体围绕启动时的世界高度上下浮动，每帧保留其他逻辑设置的水平位置。
/// </summary>
[System.Serializable]
public class BobbingObject : MonoBehaviour 
{
	public float amplitude;          // Inspector 中设置的摆动幅度，单位为 Unity 世界单位；绝对值是偏离起始高度的最大距离。
	public float speed;              // Inspector 中设置的正弦相位速度，单位为弧度/游戏秒；绝对值越大，上下往返越快。
	// Start 时记录的世界坐标；之后只使用其中的 y 作为固定的浮动中心高度。
	private Vector3 tempVal;
	// 每帧暂存当前世界坐标，只替换 y 后写回，使 x、z 跟随物体当前的位置。
	private Vector3 tempPos;

	/// <summary>
	/// 在首次更新前记录初始高度，后续浮动始终围绕这个高度计算。
	/// </summary>
	void Start () 
	{			
		tempVal = transform.position;
	}

	/// <summary>
	/// 用游戏启动后的累计时间计算本帧高度，再把保留的水平坐标一起写回。
	/// </summary>
	void Update ()
	{        
		tempPos = transform.position;
		// Sin 的结果在 -1 到 1 之间；Time.time 使用受 timeScale 影响的游戏秒数，不从物体启用时重新计时。
		tempPos.y = tempVal.y + amplitude * Mathf.Sin (speed * Time.time);
		transform.position = tempPos;
	}	
} 