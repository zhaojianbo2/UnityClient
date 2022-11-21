using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurroundCamera : MonoBehaviour
{
    //��Ұ����
    public Transform focus;

    //�����Խ�ɫ��λ��
    Vector3 RelativePosition;

    void Start()
    {
        RelativePosition = transform.position - focus.position;     //����Ϊԭ��A�����ΪB������AB=B-A��B�������������AB��
        unit = RelativePosition.magnitude;
    }

    void Update()
    {
        Follow();               //�������

        if (cameraRotateBy == CameraRotateBy.MouseVelocity)              //�����ӽ�
            DragToRotateView_Velocity();
        else
            DragToRotateView_Distance();

        OcclusionJudge();               //��Ұ�ڵ��ж�

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

    /*-----------------�������------------------*/

    void Follow()
    {
        transform.position = focus.position + RelativePosition;             //ÿһ֡�������ƶ�
    }

    /*-----------------�����ӽ�------------------*/

    //�����ת����
    public CameraRotateBy cameraRotateBy = CameraRotateBy.MouseVelocity;
    public enum CameraRotateBy
    {
        MouseVelocity,
        Distance,
    }

    //��Сˮƽ�н�
    public float MinimumDegree = 0;
    //���ˮƽ�н�
    public float MaximumDegree = 60;
    //����������ˮƽ����ļн�
    float currentAngleY;

    /*
        ����һ
        ����֮֡�������ƶ��ٶ�Ϊ���ݽ�����ת
    */

    float mouseVelocityX;
    float mouseVelocityY;
    Vector3? point1;
    //��תÿ�ȣ���һ֡����Ҫ���ٶ�
    int DragVelocityPerAngle = 170;

    //����˲������ٶ�
    float lastMouseVelocityX;
    float lastMouseVelocityY;

    void DragToRotateView_Velocity()
    {
        if (Input.GetMouseButton(0))                //������������ÿһ֡��ִ��
        {
            var point2 = Input.mousePosition;
            if (point1 != null)
            {
                mouseVelocityX = -(point1.Value.x - point2.x) / Time.deltaTime;
                mouseVelocityY = -(point1.Value.y - point2.y) / Time.deltaTime;
            }

            point1 = point2;

            float anglex = mouseVelocityX / DragVelocityPerAngle;                   //���������Ļ����ק���ٶ�ת��Ϊ�Ƕ�
            float angley = mouseVelocityY / DragVelocityPerAngle;

            currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //��������������ˮƽ����ļн�

            if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree)
                angley = 0;

            transform.RotateAround(focus.position, Vector3.up, anglex);
            transform.RotateAround(focus.position, -transform.right, angley);

            transform.LookAt(focus);                    //���û����һ�䣬����ͷת��ת�žͻ���

            RelativePosition = transform.position - focus.position;                 //�������λ��
        }

        if (Input.GetMouseButtonUp(0))       //����˲��
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
            StartCoroutine("InertialRotation");     //ͨ��Э����ʵ���ӽǵĹ�����ת������Э��ֻ��д��Update�ﲢ����ÿһ֡��������ʱ�Ż����ִ��
    }

    bool inertialRotation = false;      //�Ƿ���Ҫ�ӽǵĹ�����ת
    float maxlastMouseVelocityX = 3000;
    bool isCounterClockwise;            //��ת����
    IEnumerator InertialRotation()      //����תĩβ����һ���𽥼����Ĺ�����ת
    {
        yield return null;

        float anglex = lastMouseVelocityX / DragVelocityPerAngle;                   //���������Ļ����ק���ٶ�ת��Ϊ�Ƕ�
        float angley = lastMouseVelocityY / DragVelocityPerAngle;

        currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //��������������ˮƽ����ļн�

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
        ������
        ����֮֡�������ƶ�����Ϊ���ݽ�����ת
    */

    Vector3 Point1;
    Vector3 Point2;
    //��תÿ�ȣ���һ֡����Ҫ��ק�ľ���
    int DragDistancePerAngle = 20;

    void DragToRotateView_Distance()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        if (!(h == 0 && v == 0))                  //���˶�ʱ����ת������
        {
            DragDistancePerAngle = 17;      //����ǰ��ק������
            sactor = 10;                    //���ֺ���ק������
        }
        else                                //�˶�ʱ����ת������
        {
            DragDistancePerAngle = 8;
            sactor = 4;
        }

        if (Input.GetMouseButtonDown(0))                //������������˲�䣬��¼��ʼλ��
        {
            Point1 = Input.mousePosition;
            StartPoint = Point1;
        }

        if (Input.GetMouseButton(0))                //������������ÿһ֡��ִ��
        {
            Point2 = Input.mousePosition;
            float dx = Point2.x - Point1.x;
            float dy = Point2.y - Point1.y;

            float anglex = dx / DragDistancePerAngle;                   //���������Ļ����ק�ľ���ת��Ϊ�Ƕ�
            float angley = dy / DragDistancePerAngle;

            currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);                    //��������������ˮƽ����ļн�

            if (currentAngleY - angley > MaximumDegree || currentAngleY - angley < MinimumDegree)
                angley = 0;

            transform.RotateAround(focus.position, Vector3.up, anglex);
            transform.RotateAround(focus.position, -transform.right, angley);

            transform.LookAt(focus);                    //���û����һ�䣬����ͷת��ת�žͻ���

            RelativePosition = transform.position - focus.position;                 //�������λ��

            Point1 = Point2;
            Point2 = Vector3.zero;
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPoint = Input.mousePosition;
            if (Point1 != EndPoint)                       //������ٶ��򲻽��й�����ת
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

    Vector3 StartPoint;     //��ק���
    Vector3 EndPoint;       //��ק�յ�
    float dragX;        //ˮƽ��ק����
    float dragY;        //��ֱ��ק����
    float maxdragX = 3000;
    float sactor = 10;  //����ϵ��
    IEnumerator InertialRotation2()      //����תĩβ����һ���𽥼����Ĺ�����ת
    {
        yield return null;

        float anglex = dragX / DragDistancePerAngle / sactor;                   //���������Ļ����ק�ľ���ת��Ϊ�Ƕ�
        float angley = dragY / DragDistancePerAngle / sactor;

        currentAngleY = 90 - Vector3.Angle(-RelativePosition, Vector3.down);            //��������������ˮƽ����ļн�

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

    /*-----------------������Ұ------------------*/

    //������������
    float mouseWheelSensitivity = 30;

    //��Ұ��������
    public enum ScaleViewBy
    {
        Distance,
        FieldOfView,
        Level,
    }

    //��Ұѡ���б�
    public ScaleViewBy scaleViewBy = ScaleViewBy.Level;

    /*
        ����һ
        ����FieldOfView
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
        ������
        ���ɵ����������
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
        ������
        ȡ��ʼ����Ϊ��λ���ȣ����ÿ�ε������Դ�Ϊ��λ
        �õ�λ�������Ե�λ���ȼ��ɵõ�λ��ʸ��
    */

    //��λ����
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

    /*-----------------��Ұ�ڵ�����------------------*/

    /*
        �����Ұ���ڵ���������������ľ���
        ���ԭ��λ���ٱ��ڵ�����ָ�ԭ��λ
    */

    //��ǰ�ӽǼ���
    int currentLevel = 1;
    //ƫ����Ұ����
    int preferdLevel = 1;

    //�Ƿ���Ҫ�ָ�ԭ��λ
    bool resumable = false;

    void OcclusionJudge()
    {
        if (Physics.Raycast(transform.position, -RelativePosition.normalized, RelativePosition.magnitude - unit))               //�����λ���ڵ�
        {
            resumable = true;
            while (Physics.Raycast(transform.position, -RelativePosition.normalized, RelativePosition.magnitude - unit))
            {
                ViewPlus();
            }
        }

        if (!resumable) return;             //�������Ҫ�ָ�

        Vector3 PositionToResume = focus.position + RelativePosition.normalized * unit * preferdLevel;              //����ƫ�þ�������λ��

        if (resumable && !Physics.Raycast(PositionToResume, -RelativePosition.normalized, (preferdLevel - 1) * unit))       //ԭ��λû���ڵ�,�ָ�ԭλ
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