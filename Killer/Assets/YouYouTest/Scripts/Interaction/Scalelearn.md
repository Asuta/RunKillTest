
    public class ScaleAxis
    {
        public float Value { get; set; }
    }
    private ScaleAxis scaleAxis = new ScaleAxis();




    private void PickerScaleAxis()
    {
        // get a vector of the two hands
        Vector3 handVector = holdHandList[1].position - holdHandList[0].position;
        float angleX = Vector3.Angle(handVector, transform.right);
        // 使用Math.Min函数获取不超过90度的最小夹角
        angleX = Math.Min(angleX, 180 - angleX);
        float angleY = Vector3.Angle(handVector, transform.up);
        angleY = Math.Min(angleY, 180 - angleY);
        float angleZ = Vector3.Angle(handVector, transform.forward);
        angleZ = Math.Min(angleZ, 180 - angleZ);
        //get which axis is the smallest angle
        if (angleX < angleY && angleX < angleZ)
        {
            //rotate around the x axis
            Debug.LogError("X");
            scaleAxis.Value = transform.localScale.x;
        }
        else if (angleY < angleX && angleY < angleZ)
        {
            //rotate around the y axis
            Debug.LogError("Y");
            scaleAxis.Value = transform.localScale.y;
        }
        else if (angleZ < angleX && angleZ < angleY)
        {
            //rotate around the z axis
            Debug.LogError("Z");
            scaleAxis.Value = transform.localScale.z;
        }
        //开始缩放的第一帧，记录下当前的缩放值和两只手的距离
        if (lastScaleState != scaleState)
        {
            recordScale = scaleAxis.Value;
            recordHandDistance = Vector3.Distance(
                holdHandList[0].position,
                holdHandList[1].position
            );
        }
        //如果正在缩放，就根据两只手的距离和记录的距离来计算缩放比例
        if (scaleState == ScaleState.Scaling)
        {
            float currentHandDistance = Vector3.Distance(
                holdHandList[0].position,
                holdHandList[1].position
            );
            float scaleRate = currentHandDistance / recordHandDistance;
            Vector3 currentScale = transform.localScale;
            switch (scaleAxis.Value)
            {
                case float x when x == currentScale.x:
                    transform.localScale = new Vector3(
                        recordScale * scaleRate,
                        currentScale.y,
                        currentScale.z
                    );
                    break;
                case float y when y == currentScale.y:
                    transform.localScale = new Vector3(
                        currentScale.x,
                        recordScale * scaleRate,
                        currentScale.z
                    );
                    break;
                case float z when z == currentScale.z:
                    transform.localScale = new Vector3(
                        currentScale.x,
                        currentScale.y,
                        recordScale * scaleRate
                    );
                    break;

            }
        }
    }
