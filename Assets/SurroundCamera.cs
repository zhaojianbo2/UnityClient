using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurroundCamera : MonoBehaviour
{
    //视野中心
    public Transform focus;

    //相机相对角色的位置
    Vector3 RelativePosition;

    void Start()
    {
        RelativePosition = transform.position - focus.position;     //以人为原点A，相机为B，向量AB=B-A，B的坐标等于向量AB。
        unit = RelativePosition.magnitude;
    }

    void Update()
    {
        Follow();               //相机跟随

        if (cameraRotateBy == CameraRotateBy.MouseVelocity)              //调节视角
            DragToRotateView_Velocity();
        else
            DragToRotateView_Distance();

        OcclusionJudge();               //视野遮挡判断

        if (scaleViewBy == ScaleViewBy.Distance)
        {
            // ScrollToScaleDistance();
        }
        else if (scaleViewBy == ScaleViewBy.FieldOfView)
        {
            //ScrollToScaleView();
        }
        else if (scaleViewBy == ScaleViewBy.Level)
        {
            //ScrollToAdjustView();
        }
           
    }

    /*-----------------相机跟随------------------*/

    void Follow()
    {
        transform.position = focus.position + RelativePosition;             //每一帧都跟随移动
    }

    /*-----------------调整视角------------------*/

    //相机旋转方案
    public CameraRotateBy cameraRotateBy = CameraRotateBy.MouseVelocity;
    public enum CameraRotateBy
    {
        MouseVelocity,
        Distance,
    }

    //最小水平夹角
    public float MinimumDegree = 0;
    //最大水平夹角
    public float MaximumDegree = 60;
    //两点连线与水平方向的夹角
    float currentAngleY;

    /*
        方案一
        以两帧之间的鼠标移动速度为依据进行旋转
    */

    float mouseVelocityX;
    float mouseVelocityY;
    Vector3? point1;
    //旋转每度，在一帧中需要的速度
    int DragVelocityPerAngle = 170;

    //脱手瞬间鼠标速度
    float lastMouseVelocityX;
    float lastMouseVelocityY;

    void DragToRotateView_Velocity()
    {
        if (Input.GetMouseButton(0))                //按下鼠标左键的每一帧都执行
        {
            var point2 = Input.mousePosition;
            if (point1 != null)
            {
                mouseVelocityX = -(point1.Value.x - point2.x) / Time.deltaTime;
                mouseVelocityY = -(point1.Value.y - point2.y) / Time.deltaTime;
            }

            point1 = point2;

            float anglex = mouseVelocityX / DragVelocityPerAngle;                   //将鼠标在屏幕上拖拽的速度转化为角度
            float angley = mouseVelocityY / DragVelocityPerAngle;

            currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //计算两点连线与水平方向的夹角

            if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree)
                angley = 0;

            transform.RotateAround(focus.position, Vector3.up, anglex);
            transform.RotateAround(focus.position, -transform.right, angley);

            transform.LookAt(focus);                    //如果没有这一句，摄像头转着转着就会歪

            RelativePosition = transform.position - focus.position;                 //更新相对位置
        }

        if (Input.GetMouseButtonUp(0))       //脱手瞬间
        {
            point1 = null;

            inertialRotation = true;
            lastMouseVelocityX = mouseVelocityX;
            lastMouseVelocityY = mouseVelocityY;
            if (lastMouseVelocityX > maxlastMouseVelocityX) lastMouseVelocityX = maxlastMouseVelocityX;
            else if (lastMouseVelocityX < -maxlastMouseVelocityX) lastMouseVelocityX = -maxlastMouseVelocityX;

            if (lastMouseVelocityX > 0) isCounterClockwise = true;
            else if (lastMouseVelocityX < 0) isCounterClockwise = false;
            //print(lastMouseVelocityX);
        }


        if (inertialRotation == true)
            StartCoroutine("InertialRotation");     //通过协程来实现视角的惯性旋转，调用协程只有写在Update里并且在每一帧都被调用时才会继续执行
    }

    bool inertialRotation = false;      //是否需要视角的惯性旋转
    float maxlastMouseVelocityX = 3000;
    bool isCounterClockwise;            //旋转方向
    IEnumerator InertialRotation()      //在旋转末尾补上一个逐渐减缓的惯性旋转
    {
        yield return null;

        float anglex = lastMouseVelocityX / DragVelocityPerAngle;                   //将鼠标在屏幕上拖拽的速度转化为角度
        float angley = lastMouseVelocityY / DragVelocityPerAngle;

        currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //计算两点连线与水平方向的夹角

        if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree + 10)
            angley = 0;

        lastMouseVelocityX -= lastMouseVelocityX * 0.08f;
        lastMouseVelocityY -= lastMouseVelocityY * 0.08f;

        //print(lastMouseVelocityX);

        if ((isCounterClockwise && (anglex < 1)) || !isCounterClockwise && (anglex > -1))
        {
            StopCoroutine("InertialRotation");
            inertialRotation = false;
        }
        transform.RotateAround(focus.position, Vector3.up, anglex / 3);
        transform.RotateAround(focus.position, -transform.right, Mathf.Abs(angley / 25));
        transform.LookAt(focus);
        RelativePosition = transform.position - focus.position;
    }

    /*
        方案二
        以两帧之间的鼠标移动距离为依据进行旋转
    */

    Vector3 Point1;
    Vector3 Point2;
    //旋转每度，在一帧中需要拖拽的距离
    int DragDistancePerAngle = 20;

    void DragToRotateView_Distance()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        if (!(h == 0 && v == 0))                  //不运动时的旋转灵敏度
        {
            DragDistancePerAngle = 17;      //松手前拖拽灵敏度
            sactor = 10;                    //松手后拖拽灵敏度
        }
        else                                //运动时的旋转灵敏度
        {
            DragDistancePerAngle = 8;
            sactor = 4;
        }

        if (Input.GetMouseButtonDown(0))                //按下鼠标左键的瞬间，记录起始位置
        {
            Point1 = Input.mousePosition;
            StartPoint = Point1;
        }

        if (Input.GetMouseButton(0))                //按下鼠标左键的每一帧都执行
        {
            Point2 = Input.mousePosition;
            float dx = Point2.x - Point1.x;
            float dy = Point2.y - Point1.y;

            float anglex = dx / DragDistancePerAngle;                   //将鼠标在屏幕上拖拽的距离转化为角度
            float angley = dy / DragDistancePerAngle;

            currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);                    //计算两点连线与水平方向的夹角

            if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree)
                angley = 0;

            transform.RotateAround(focus.position, Vector3.up, anglex);
            transform.RotateAround(focus.position, -transform.right, angley);

            transform.LookAt(focus);                    //如果没有这一句，摄像头转着转着就会歪

            RelativePosition = transform.position - focus.position;                 //更新相对位置

            Point1 = Point2;
            Point2 = Vector3.zero;
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPoint = Input.mousePosition;
            if (Point1 != EndPoint)                       //鼠标无速度则不进行惯性旋转
                inertialRotation = true;
            dragX = EndPoint.x - StartPoint.x;
            dragY = EndPoint.y - StartPoint.y;
            if (dragX > maxdragX) dragX = maxdragX;
            else if (dragX < -maxdragX) dragX = -maxdragX;

            if (dragX > 0) isCounterClockwise = true;
            else if (dragX < 0) isCounterClockwise = false;
            print(dragX);
        }

        if (inertialRotation == true)
            StartCoroutine("InertialRotation2");
    }

    Vector3 StartPoint;     //拖拽起点
    Vector3 EndPoint;       //拖拽终点
    float dragX;        //水平拖拽距离
    float dragY;        //垂直拖拽距离
    float maxdragX = 3000;
    float sactor = 10;  //惯性系数
    IEnumerator InertialRotation2()      //在旋转末尾补上一个逐渐减缓的惯性旋转
    {
        yield return null;

        float anglex = dragX / DragDistancePerAngle / sactor;                   //将鼠标在屏幕上拖拽的距离转化为角度
        float angley = dragY / DragDistancePerAngle / sactor;

        currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //计算两点连线与水平方向的夹角

        if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree + 10)
            angley = 0;


        dragX -= dragX * 0.05f;
        dragY -= dragY * 0.05f;

        print(dragX);

        if ((isCounterClockwise && (anglex < 1)) || !isCounterClockwise && (anglex > -1))
        {
            StopCoroutine("InertialRotation2");
            inertialRotation = false;
        }
        transform.RotateAround(focus.position, Vector3.up, anglex / 4);
        transform.RotateAround(focus.position, -transform.right, Mathf.Abs(angley / 4));
        transform.LookAt(focus);
        RelativePosition = transform.position - focus.position;
    }

    /*-----------------调整视野------------------*/

    //鼠标滚轮灵敏度
    float mouseWheelSensitivity = 30;

    //视野调整方案
    public enum ScaleViewBy
    {
        Distance,
        FieldOfView,
        Level,
    }

    //视野选择列表
    public ScaleViewBy scaleViewBy = ScaleViewBy.Level;

    /*
        方案一
        调节FieldOfView
    */

    float MinFieldOfView = 20f;
    float MaxFieldOfView = 100f;

    void ScrollToScaleView()
    {
        if (Input.GetAxis("Mouse ScrollWheel") == 0) return;

        GetComponent<Camera>().fieldOfView = GetComponent<Camera>().fieldOfView - Input.GetAxis("Mouse ScrollWheel") * mouseWheelSensitivity;
        GetComponent<Camera>().fieldOfView = Mathf.Clamp(GetComponent<Camera>().fieldOfView, MinFieldOfView, MaxFieldOfView);
    }

    /*
        方案二
        自由调节相机距离
    */

    float MinViewDistance = 1;
    float MaxViewDistance = 4;

    void ScrollToScaleDistance()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (RelativePosition.magnitude <= MinViewDistance) return;

            transform.Translate(-RelativePosition / mouseWheelSensitivity * 10, Space.World);
            RelativePosition = transform.position - focus.position;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (RelativePosition.magnitude >= MaxViewDistance) return;

            transform.Translate(RelativePosition / mouseWheelSensitivity * 10, Space.World);
            RelativePosition = transform.position - focus.position;
        }
    }

    /*
        方案三
        取初始距离为单位长度，随后每次调整都以此为单位
        用单位向量乘以单位长度即可得到位移矢量
    */

    //单位长度
    float unit;

    void ScrollToAdjustView()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            ViewPlus();
            preferdLevel = currentLevel;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            ViewMinus();
            preferdLevel = currentLevel;
        }
    }

    void ViewPlus()
    {
        if (RelativePosition.magnitude <= MinViewDistance) return;
        transform.Translate(-RelativePosition.normalized * unit, Space.World);
        RelativePosition = transform.position - focus.position;
        currentLevel--;
    }

    void ViewMinus()
    {
        if (RelativePosition.magnitude >= MaxViewDistance) return;
        transform.Translate(RelativePosition.normalized * unit, Space.World);
        RelativePosition = transform.position - focus.position;
        currentLevel++;
    }

    /*-----------------视野遮挡处理------------------*/

    /*
        如果视野被遮挡，就逐级拉近相机的距离
        如果原机位不再被遮挡，则恢复原机位
    */

    //当前视角级别
    int currentLevel = 1;
    //偏好视野级别
    int preferdLevel = 1;

    //是否需要恢复原机位
    bool resumable = false;

    void OcclusionJudge()
    {
        if (Physics.Raycast(transform.position, -RelativePosition.normalized, RelativePosition.magnitude - unit))               //如果机位被遮挡
        {
            resumable = true;
            while (Physics.Raycast(transform.position, -RelativePosition.normalized, RelativePosition.magnitude - unit))
            {
                ViewPlus();
            }
        }

        if (!resumable) return;             //如果不需要恢复

        Vector3 PositionToResume = focus.position + RelativePosition.normalized * unit * preferdLevel;              //计算偏好距离所在位置

        if (resumable && !Physics.Raycast(PositionToResume, -RelativePosition.normalized, (preferdLevel - 1) * unit))       //原机位没被遮挡,恢复原位
        {
            while (currentLevel != preferdLevel)
            {
                ViewMinus();
            }
            resumable = false;
        }
    }

    //todo
}