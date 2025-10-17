同时按住“trigger”和"grab"两个按键的拖拽移动

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EditorMove : MonoBehaviour
{
    private OVRInput.Controller lController = OVRInput.Controller.LTouch;
    private OVRInput.Controller RController = OVRInput.Controller.RTouch;
    public Transform rigT; // tracking space
    public Transform headT; // head(center相机位置)
    private Vector3 rigRecordPos; //rigT记录位置(用于拖拽移动，按下时记录位置)
    public float moveSpeed; //移动速度
    public Transform leftHand; //左手
    private Vector3 leftHandRecordPos; //左手记录位置
    public Transform rightHand; //右手
    private Vector3 rightHandRecordPos; //右手记录位置
    // Start is called before the first frame update
    void Start() { }
    // Update is called once per frame
    void Update()
    {
        moveSpeed = rigT.localScale.x;
        StickRotate();
        //拖拽移动
        DragMove();
        //左摇杆移动
        StrickMove();
        //InputTest();
    }
    private void StickRotate()
    {
        if (UIGameManager.Instance._isHovering)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp, RController))
            {
                UIGameManager.Instance.ScrollMove(1);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown, RController))
            {
                UIGameManager.Instance.ScrollMove(-1);
            }
        }
        else
        {
            //右手摇杆进行转向，每次旋转45度(以头部（headT）为旋转中心)
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight, RController))
            {
                rigT.RotateAround(headT.position, Vector3.up, 45);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft, RController))
            {
                rigT.RotateAround(headT.position, Vector3.up, -45);
            }
        }

    }
    private void StrickMove()
    {
        //左摇杆移动
        Vector2 move = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, lController);
        // 创建一个只有Y轴旋转的四元数
        Quaternion rotationY = Quaternion.Euler(0, leftHand.rotation.eulerAngles.y, 0);
        // 使用新的四元数旋转移动向量
        Vector3 dir = rotationY * new Vector3(move.x, 0, move.y);
        dir = new Vector3(dir.x, 0, dir.z);
        rigT.position += dir * moveSpeed * Time.deltaTime;
    }
    private void DragMove()
    {
        //rightHand move  record position
        if (
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, RController)
            && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, RController)
        )
        {
            rightHandRecordPos = rightHand.localPosition;
            rigRecordPos = rigT.position;
        }
        if (
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, RController)
            && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, RController)
        )
        {
            rightHandRecordPos = rightHand.localPosition;
            rigRecordPos = rigT.position;
        }
        //rightHand move  moveing
        if (
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, RController)
            && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, RController)
        )
        {
            Vector3 offset = rightHand.localPosition - rightHandRecordPos;
            offset = Quaternion.Euler(0, rigT.eulerAngles.y, 0) * offset;
            rigT.position = rigRecordPos - offset * moveSpeed;
        }




        //leftHand move record position
        if (
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, lController)
            && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, lController)
        )
        {
            leftHandRecordPos = leftHand.localPosition;
            rigRecordPos = rigT.position;
        }
        if (
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, lController)
            && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, lController)
        )
        {
            leftHandRecordPos = leftHand.localPosition;
            rigRecordPos = rigT.position;
        }
        //leftHand move moveing
        if (
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, lController)
            && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, lController)
        )
        {
            Vector3 offset = leftHand.localPosition - leftHandRecordPos;
            offset = Quaternion.Euler(0, rigT.eulerAngles.y, 0) * offset;
            rigT.position = rigRecordPos - offset * moveSpeed;
        }
    }
}
