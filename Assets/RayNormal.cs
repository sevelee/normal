using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayNormal : MonoBehaviour {

	#region Public Variables

	public GameObject markGOBJ;
	public GameObject planeGOBJ;

	public float theta = 0f;
	public float phi = 0f;
	public float psi = 0f;
	public float upRadius = 20f;
	public float downRadius = 30f;

	public float delatZ = 0f;

	public Vector3 upPosition;
	public Vector3 unityUpPosition;
	#endregion

	#region Private Variables

	Vector3 xyzNormal;
	Vector3 touchNormal;
	Vector3 touchNormalLocal;

	//upPoints is A, downPosition is B.
	Vector3[] upPoints;
	Vector3[] downPoints;
	float[] l1, l2, l3;
	float[] beta1, beta2;
	float[] beta3;

	Vector3[] BA,BO;

	Vector3 u,v,w;

	float sinpsi = 0f;
	float cospsi = 0f;
	float sinTheta = 0f;
	float sinPhi = 0f;
	float cosPhi = 0f;

	float[] phis;
	#endregion

	void Awake()
	{
		// init phis
		phis = new float[3];
		phis [0] = 0f;
		phis [1] = 2 * Mathf.PI / 3;
		phis [2] = 4 * Mathf.PI / 3;

		upPosition = new Vector3 (0f, 0f, 0f);
		unityUpPosition = new Vector3 (0f, 0f, 0f);

		upPoints = new Vector3[3];
		downPoints = new Vector3[3];
		l1 = new float[3];
		l2 = new float[3];
		l3 = new float[3];

		beta1 = new float[3];
		beta2 = new float[3];
		beta3 = new float[3];

		BA = new Vector3[3];
		BO = new Vector3[3];

		for (int i = 0; i < 3; ++i)
		{
			l1 [i] = 30;
			l2 [i] = 30;
		}

		u = Vector3.zero;
		v = Vector3.zero;
		w = Vector3.zero;
	}

	void Start () {
		touchNormal = new Vector3 (0f, 0f, 0f);
	}
	
	void Update () {
		Ray myRay = new Ray (this.transform.position, this.transform.up);
		RaycastHit myHit;
		if (Physics.Raycast(myRay, out myHit))
		{
			if (myHit.collider.tag == "hit")
			{
				markGOBJ.transform.position = myHit.point;
				xyzNormal.x = myHit.normal.x;
				xyzNormal.y = myHit.normal.z;
				xyzNormal.z = myHit.normal.y;
				xyzNormal = - xyzNormal;
				touchNormal = xyzNormal;
				touchNormalLocal = transform.InverseTransformDirection (touchNormal);
				delatZ = myHit.distance;

				//注意目前算出来的坐标位置都是以下方的平面坐标系为基准的，并不是 unity 中的世界坐标
				upPosition.x = (upRadius * Mathf.Sin(2 * psi) * (Mathf.Cos(theta) - 1)) / 2;
				upPosition.y = (upRadius * Mathf.Cos(2 * psi) * (Mathf.Cos(theta) - 1)) / 2;
				upPosition.z = myHit.distance;

				//从右手坐标系到 unity 的左手坐标系
				unityUpPosition.x = upPosition.x;
				unityUpPosition.y = upPosition.z;
				unityUpPosition.z = upPosition.y;
			}
		}

		//计算旋转的欧拉角
		theta = Mathf.Acos (touchNormalLocal.z);
		sinTheta = Mathf.Sin (theta);
		if (touchNormalLocal.x != 0)
		{
			sinpsi = touchNormalLocal.x / sinTheta;
			psi = Mathf.Asin (sinpsi);
		} 
		else if (touchNormalLocal.y != 0)
		{
			cospsi = (-touchNormalLocal.y / sinTheta);
			cospsi = Mathf.Clamp (cospsi, -1, 1);
			psi = Mathf.Acos (cospsi);
		}
		phi = -psi;

		//计算动坐标系的 u, v, w
		u.x = Mathf.Cos (psi) * Mathf.Cos (phi) - Mathf.Sin (psi) * Mathf.Cos (theta) * Mathf.Sin (phi);
		u.y = Mathf.Sin (psi) * Mathf.Cos (phi) - Mathf.Cos (psi) * Mathf.Cos (theta) * Mathf.Sin (phi);
		u.z = Mathf.Sin (theta) * Mathf.Sin (phi);

		v.x = - Mathf.Cos (psi) * Mathf.Sin (phi) - Mathf.Sin (psi) * Mathf.Cos (theta) * Mathf.Cos (phi);
		v.y = - Mathf.Sin (psi) * Mathf.Sin (phi) + Mathf.Cos (psi) * Mathf.Cos (theta) * Mathf.Cos (phi);
		v.z =   Mathf.Sin (theta) * Mathf.Cos (phi);

		w = touchNormalLocal;

		//更新 Ai 的坐标
		for (int i = 0; i < 3; ++i)
		{
			upPoints [i].x = upRadius * Mathf.Cos (phis [i]) * u.x + upRadius * Mathf.Sin (phis [i]) * v.x + upPosition.x;
			upPoints [i].y = upRadius * Mathf.Cos (phis [i]) * u.y + upRadius * Mathf.Sin (phis [i]) * v.y + upPosition.y;
			upPoints [i].z = upRadius * Mathf.Cos (phis [i]) * u.z + upRadius * Mathf.Sin (phis [i]) * v.z + upPosition.z;
		}

		//更新 Bi 的坐标
		for (int i = 0; i < 3; ++i)
		{
			downPoints [i] = new Vector3 (downRadius * Mathf.Cos (phis [i]), downRadius * Mathf.Sin (phis [i]), 0);
		}

		//更新 l3 的长度
		for (int i = 0; i < 3; ++i)
		{
			l3 [i] = Vector3.Distance (upPoints [i], downPoints [i]);
			Debug.LogFormat ("The l3,{0} is {1} ", i, l3[i]);
		}

		//计算 beta1 的大小
		for (int i = 0; i < 3; ++i)
		{
			float upPart = l1 [i] * l1 [i] + l3 [i] * l3 [i] - l2 [i] * l2 [i];
			float downPart = 2 * l1 [i] * l3 [i];
			beta1 [i] = Mathf.Acos (upPart / downPart);
			Debug.LogFormat ("The beta1,{0} is {1} ", i, Mathf.Rad2Deg * beta1 [i]);
		}

		//更新 BA
		for (int i = 0; i < 3; ++i)
		{
			BA [i] = upPoints [i] - downPoints [i];
			Debug.LogFormat ("ba[{0}] is {1} ", i, BA [i]);
		}

		//更新 BO
		for (int i = 0; i < 3; ++i)
		{
			BO [i] = - downPoints [i];
		}

		//更新 beta2
		for (int i = 0; i < 3; ++i)
		{
			beta2 [i] = Mathf.Acos (Vector3.Dot (BA [i], BO [i]) / (BA [i].magnitude * BO [i].magnitude));
		}

		//更新 beta3
		for (int i = 0; i < 3; ++i)
		{
			beta3 [i] = beta1 [i] + beta2 [i];
		}

		//Log 三个角度
		Debug.Log ("psi = "   + psi   * Mathf.Rad2Deg);
		Debug.Log ("theta = " + theta * Mathf.Rad2Deg);
		Debug.Log ("phi = "   + phi   * Mathf.Rad2Deg);
		Debug.LogFormat ("Up center position is {0}", upPosition);
		Debug.LogFormat ("beta 1,1 is {0}, beta 1,2 is {1}, beta 1,3 is {2} ", beta1[0] * Mathf.Rad2Deg, beta1[1] * Mathf.Rad2Deg, beta1[2] * Mathf.Rad2Deg);
		Debug.LogFormat ("beta 2,1 is {0}, beta 2,2 is {1}, beta 2,3 is {2} ", beta2[0] * Mathf.Rad2Deg, beta2[1] * Mathf.Rad2Deg, beta2[2] * Mathf.Rad2Deg);
		Debug.LogFormat ("beta 3,1 is {0}, beta 3,2 is {1}, beta 3,3 is {2} ", beta3[0] * Mathf.Rad2Deg, beta3[1] * Mathf.Rad2Deg, beta3[2] * Mathf.Rad2Deg);

		rotatePlane ();
	}

	void rotatePlane()
	{
		planeGOBJ.transform.rotation = this.transform.rotation;
//		planeGOBJ.transform.rotation = 
//			Quaternion.AngleAxis ( - psi * Mathf.Rad2Deg,   this.transform.up) *
//			Quaternion.AngleAxis ( - theta * Mathf.Rad2Deg, this.transform.right) *
//			Quaternion.AngleAxis ( - phi * Mathf.Rad2Deg,   this.transform.up);

		//因为是 self ，而且在 unity 里放在了一个地方。那么这个 self 就是底座的三个坐标轴。
		planeGOBJ.transform.Rotate (new Vector3 (0, - psi * Mathf.Rad2Deg, 0), Space.Self);
		planeGOBJ.transform.Rotate (new Vector3 ( - theta * Mathf.Rad2Deg, 0, 0), Space.Self);
		planeGOBJ.transform.Rotate (new Vector3 (0, - phi * Mathf.Rad2Deg, 0), Space.Self);
		Debug.Log (planeGOBJ.transform.rotation);
	}
}