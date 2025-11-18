
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


这里在对值进行case比较的时候，实际上并没有比较引用变量，而是比较的值变量

但是比如说在修改了X轴的一帧之后，因为X轴的值已经变了，下次就不会修改X轴的值了，就会继续轮询到下个轴，直到找到正确的轴而已
而以为一帧时间改变的很小，基本看不出来，所以看起来就像没有bug一样。。。。

下面是一个来自gpt的另一个储存引用变量的粒子
public delegate float GetValue();
public delegate void SetValue(float value);

public class ScaleAxis
{
    public GetValue Get { get; set; }
    public SetValue Set { get; set; }
}

// 示例代码
void UpdateScale()
{
    ScaleAxis scaleAxis = new ScaleAxis();
    Vector3 currentScale = transform.localScale;
    float recordScale = 1.0f; // 假设某个记录的比例值
    float scaleRate = 1.2f;   // 假设某个缩放比例

    if (angleX < angleY && angleX < angleZ)
    {
        Debug.LogError("X");
        scaleAxis.Get = () => transform.localScale.x;
        scaleAxis.Set = (value) => {
            var scale = transform.localScale;
            scale.x = value;
            transform.localScale = scale;
        };
    }
    else if (angleY < angleX && angleY < angleZ)
    {
        Debug.LogError("Y");
        scaleAxis.Get = () => transform.localScale.y;
        scaleAxis.Set = (value) => {
            var scale = transform.localScale;
            scale.y = value;
            transform.localScale = scale;
        };
    }
    else if (angleZ < angleX && angleZ < angleY)
    {
        Debug.LogError("Z");
        scaleAxis.Get = () => transform.localScale.z;
        scaleAxis.Set = (value) => {
            var scale = transform.localScale;
            scale.z = value;
            transform.localScale = scale;
        };
    }

    float currentValue = scaleAxis.Get(); // 获取当前值
    scaleAxis.Set(currentValue * scaleRate); // 设置新值
}







