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
    SocketDesc socketDesc;  //  ���� ��ũ����

    Dictionary<string, Spawn> spawns;
    Spawn mySpawn;          //  ������ ����
    Dictionary<string, WorldItem> worldItems;

    void Start()
    {
        //  ���� ��ũ���͸� �����մϴ�.
        socketDesc = SocketDesc.Create();

        //  spawn���� ������ spawns�� �����մϴ�.
        spawns = new Dictionary<string, Spawn>();
        //  world item���� ������ worldItems�� �����մϴ�.
        worldItems = new Dictionary<string, WorldItem>();
    }
    void Update()
    {
        //  ���� ������ �����Ǿ� ���� �ʾҴٸ� �ƹ� �۾� �� �ϵ��� �մϴ�.
        if(mySpawn == null) return;

        //  WŰ�� ������ ����
        if(Input.GetKeyDown(KeyCode.W)) { mySpawn.speed = 1.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.W)) { mySpawn.speed = 0.0f; SendMove(); }
        if(Input.GetKeyDown(KeyCode.S)) { mySpawn.speed = -0.5f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.S)) { mySpawn.speed = 0.0f; SendMove(); }
        //  AŰ�� ������ ��ȸ��, DŰ�� ������ ��ȸ��
        //  t ������ �� �������� �ð� (�ʴ���)�� ����
        if(Input.GetKeyDown(KeyCode.A)) { mySpawn.aspeed = -90.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.A)) { mySpawn.aspeed = 0.0f; SendMove(); }
        if(Input.GetKeyDown(KeyCode.D)) { mySpawn.aspeed = 90.0f; SendMove(); }
        else if(Input.GetKeyUp(KeyCode.D)) { mySpawn.aspeed = 0.0f; SendMove(); }
        
        //  ���콺 �� ���� �о�ͼ� zoom�� ����
        zoom += Input.mouseScrollDelta.y*0.1f;
        if(zoom<-1.0f) zoom = -1.0f; else if(zoom > 1.0f) zoom = 1.0f;
        var t = Time.deltaTime;

        //  ���콺 ��ư 0�� ������ ���콺�� ��ġ�� ǥ��
        if(Input.GetMouseButtonDown(0))
        {
            //  1. ī�޶� ��ġ�κ��� �ش� ���콺 ��ġ�� ���� ray ����
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //  2. �ش� ray�� �̿��Ͽ� �浹�˻�
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
        
        //  ���콺 ��ư 1�� Ŭ���� ���¿��� �� ó��
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
        //  ī�޶� ��ġ
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
        //  1. ���� ��ũ���Ͱ� �������� ������ �ƹ����� ���ϱ�
	    if(socketDesc == null) return;
        //  2. processNetwork�� true�� �ƴ϶�� �ƹ����� ���ϱ�
        if(!socketDesc.ProcessNetwork()) return;
        //  3. ��Ŷ ��������
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
            //  1. ���ο� ���ӿ�����Ʈ ����
            var go = new GameObject();
            //  2. �� ������Ʈ�� WorldItem ������Ʈ�� ���
            var wi = go.AddComponent<WorldItem>();
            //  3. ���� ������Ʈ�� ����
            wi.CreateItem(ss[1], float.Parse(ss[2]), float.Parse(ss[3]), float.Parse(ss[4]));
            //  4. worldItems��� �ڷᱸ���� �ش� ���� �������� ���
            worldItems[ss[1]] = wi;
        }
        else if(ss[0] == "update")
        {
            //  1. �̸��� ������ worlditems �׸��� ã�ƺ��ϴ�.
            if(worldItems.ContainsKey(ss[1]))
            {
                //  2. ã�� �׸񿡼� UpdateItem �Լ��� ȣ���մϴ�.
                var wi = worldItems[ss[1]];
                wi.UpdateItem(ss[1], ss[2]);
            }
        }
	}
	//  ���� ��ư�� ������ ���� �۾�
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

        //  Spawn �����
        //  1. Make a Empty Game object
        var go = new GameObject(name.text);
        //  2. Avatar component �߰��ϱ�
        mySpawn = go.AddComponent<Spawn>();
        //  3. Avatar �����ϱ�
        var model = Random.Range(0, 2);
        mySpawn.CreateAvatar(name.text, model);
        var hair = Random.Range(0, 4);
        var body = Random.Range(0, 4);
        var legs = Random.Range(0, 4);
        var shoes = Random.Range(0, 4);
        mySpawn.ChangeLook(hair, body, legs, shoes);

        //  �����ϱ�
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
