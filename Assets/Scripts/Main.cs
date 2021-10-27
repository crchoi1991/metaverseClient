using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    float distance = 5.0f;  //  camera distance
    float zoom;             //  camera zoom
    float camoffset;        //  camera offset angle
    Vector2 lastMousePos;   //  last mouse position
    SocketDesc socketDesc;  //  소켓 디스크립터

    Dictionary<string, Spawn> spawns;
    Spawn mySpawn;          //  본인의 스폰
    Dictionary<string, WorldItem> worldItems;

    void Start()
    {
        //  소켓 디스크립터를 생성합니다.
        socketDesc = SocketDesc.Create();

        //  spawn들을 관리할 spawns를 생성합니다.
        spawns = new Dictionary<string, Spawn>();
        //  world item들을 관리할 worldItems를 생성합니다.
        worldItems = new Dictionary<string, WorldItem>();
    }
    void Update()
    {
        //  현재 스폰이 생성되어 있지 않았다면 아무 작업 안 하도록 합니다.
        if(mySpawn == null) return;

        //  W키가 눌리면 전진
        if(Input.GetKeyDown(KeyCode.W)) { mySpawn.speed = 1.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.W)) { mySpawn.speed = 0.0f; SendMove(); }
        if(Input.GetKeyDown(KeyCode.S)) { mySpawn.speed = -0.5f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.S)) { mySpawn.speed = 0.0f; SendMove(); }
        //  A키가 눌리면 좌회전, D키가 눌리면 우회전
        //  t 변수에 한 프레임의 시간 (초단위)를 저장
        if(Input.GetKeyDown(KeyCode.A)) { mySpawn.aspeed = -90.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.A)) { mySpawn.aspeed = 0.0f; SendMove(); }
        if(Input.GetKeyDown(KeyCode.D)) { mySpawn.aspeed = 90.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.D)) { mySpawn.aspeed = 0.0f; SendMove(); }
        
        //  마우스 휠 값을 읽어와서 zoom에 적용
        zoom += Input.mouseScrollDelta.y*0.1f;
        if(zoom<-1.0f) zoom = -1.0f; else if(zoom > 1.0f) zoom = 1.0f;
        var t = Time.deltaTime;

        //  마우스 버튼 0이 눌리면 마우스의 위치를 표시
        if(Input.GetMouseButtonDown(0))
        {
            //  1. 카메라 위치로부터 해당 마우스 위치로 가는 ray 생성
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //  2. 해당 ray를 이용하여 충돌검사
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                Transform ht = hit.transform;
                WorldItem wi = null;
                while( ht != null )
                {
                    if(ht.gameObject.TryGetComponent<WorldItem>(out wi)) break;
                    ht = ht.parent;
                }
                if(wi != null)
                {
                    Debug.LogFormat("Hit : {0}", wi.name);
                    var mesg = string.Format("action {0} {1} {2}", 
                        mySpawn.name, wi.name, hit.transform.name);
                    socketDesc.Send(Encoding.UTF8.GetBytes(mesg));
                }
            }
        }
        
        //  마우스 버튼 1이 클릭된 상태에서 팬 처리
        if(Input.GetMouseButtonDown(1)) lastMousePos = Input.mousePosition;
        else if(Input.GetMouseButtonUp(1)) lastMousePos = Vector2.zero;
        if(lastMousePos != Vector2.zero)
        {
            camoffset += Input.mousePosition.x - lastMousePos.x;
            lastMousePos = Input.mousePosition;
        }
        else if(mySpawn.speed != 0.0f)
        {
            if(camoffset < 0.0f)
            {
                camoffset += 0.1f;
                if(camoffset > 0.0f) camoffset = 0.0f;
            }
            else if(camoffset > 0.0f)
            {
                camoffset -= 0.1f;
                if(camoffset < 0.0f) camoffset = 0.0f;
            }
        }
        //  카메라 위치
        var camd = distance*Mathf.Pow(2.0f, zoom);
        var rad = Mathf.Deg2Rad * (mySpawn.direction+camoffset);
        var cdirv = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
        Camera.main.transform.localPosition = mySpawn.transform.localPosition - 
            cdirv*camd*Mathf.Cos(30*Mathf.PI/180.0f) + 
            (new Vector3(0, camd*Mathf.Sin(30*Mathf.PI/180.0f)+1.8f, 0));
        Camera.main.transform.localEulerAngles = new Vector3(30.0f, (mySpawn.direction+camoffset), 0);
    }
	void FixedUpdate()
	{
        //  1. 소켓 디스크립터가 존재하지 않으면 아무짓도 안하기
	    if(socketDesc == null) return;
        //  2. processNetwork이 true가 아니라면 아무짓도 안하기
        if(!socketDesc.ProcessNetwork()) return;
        //  3. 패킷 가져오기
        var packet = Encoding.UTF8.GetString(socketDesc.GetPacket());
        Debug.Log(packet);
        var ss = packet.Split();
        if(ss[0] == "join")
        {
            if(!spawns.ContainsKey(ss[1]))
            {
                var go = new GameObject();
                var spawn = go.AddComponent<Spawn>();
                spawns[ss[1]] = spawn;
            }
        }
        else if(ss[0] == "avatar")
        {
            if(spawns.ContainsKey(ss[1]))
            {
                var spawn = spawns[ss[1]];
                if(spawn != mySpawn) spawn.CreateAvatar(ss[1], int.Parse(ss[2]));
            }
        }
        else if(ss[0] == "look")
        {
            if(spawns.ContainsKey(ss[1]))
            {
                var spawn = spawns[ss[1]];
                spawn.ChangeLook(int.Parse(ss[2]), int.Parse(ss[3]), int.Parse(ss[4]), int.Parse(ss[5]));
            }
        }
        else if(ss[0] == "worlddata")
        {
            //  1. 새로운 게임오브젝트 생성
            var go = new GameObject();
            //  2. 이 오브젝트를 WorldItem 컴포넌트를 등록
            var wi = go.AddComponent<WorldItem>();
            //  3. 실제 오브젝트를 생성
            wi.CreateItem(ss[1], float.Parse(ss[2]), float.Parse(ss[3]), float.Parse(ss[4]));
            //  4. worldItems라는 자료구조에 해당 월드 아이템을 등록
            worldItems[ss[1]] = wi;
        }
        else if(ss[0] == "update")
        {
            //  1. 이름을 가지고 worlditems 항목을 찾아봅니다.
            if(worldItems.ContainsKey(ss[1]))
            {
                //  2. 찾은 항목에서 UpdateItem 함수를 호출합니다.
                var wi = worldItems[ss[1]];
                wi.UpdateItem(ss[1], ss[2]);
            }
        }
	}
	//  접속 버튼이 눌렸을 때의 작업
	public void OnButtonConnect()
    {
        //  1. Find "LoginWindow"
        var loginWindow = GameObject.Find("LoginWindow");
        //  2. Find "InputField"
        var name = loginWindow.transform.Find("InputField").GetComponent<InputField>();
        //  3. Print result
        Debug.LogFormat("Connet with {0}.", name.text);
        //  4. Hide Login window
        loginWindow.SetActive(false);

        //  Spawn 만들기
        //  1. Make a Empty Game object
        var go = new GameObject(name.text);
        //  2. Avatar component 추가하기
        mySpawn = go.AddComponent<Spawn>();
        //  3. Avatar 생성하기
        var model = Random.Range(0, 2);
        mySpawn.CreateAvatar(name.text, model);
        var hair = Random.Range(0, 4);
        var body = Random.Range(0, 4);
        var legs = Random.Range(0, 4);
        var shoes = Random.Range(0, 4);
        mySpawn.ChangeLook(hair, body, legs, shoes);

        //  접속하기
        if(socketDesc.Connect("127.0.0.1", 8888))
        {
            Debug.Log("Connected");
            socketDesc.Send(Encoding.UTF8.GetBytes(string.Format("join {0}", name.text)));
            socketDesc.Send(Encoding.UTF8.GetBytes(string.Format("avatar {0} {1}", name.text, model)));
            socketDesc.Send(Encoding.UTF8.GetBytes(string.Format("look {0} {1} {2} {3} {4}", 
                name.text, hair, body, legs, shoes)));
            spawns[name.text] = mySpawn;
        }
        else
        {
            Debug.LogError("Connection is failed");
        }
    }
    void SendMove()
    {
        var mesg = string.Format("move {0} {1} {2} {3} {4} {5}",
            mySpawn.name, mySpawn.transform.localPosition.x, mySpawn.transform.localPosition.z,
            mySpawn.direction, mySpawn.speed, mySpawn.aspeed);
        socketDesc.Send(Encoding.UTF8.GetBytes(mesg));
    }
}
