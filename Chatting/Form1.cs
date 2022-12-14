using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Chatting
{
    public partial class Form1 : Form
    {
        string serverIP, dialogName;
        int serverPort;
        bool isAlive = false;
        // NetworkStream: 데이터를 주고 받는데 사용한다.
        NetworkStream ns = null;
        // StreamReader: 스트림에서 문자를 읽는다.
        StreamReader sr = null;
        // StreamWriter: 문자열 데이터를 스트림에 저장하는 데 쓰인다.
        StreamWriter sw = null;
        // TcpClient 클래스는 클라이언트에서는 TcpClient가 서버에 연결 요청을 하는 역할을 한다.
        // 서버에서는 클라이언트의 요청을 수락하면 클라이언트와 통신을 할 때 사용하는 TcpClient의 인스턴스가 반환된다.
        TcpClient client = null;

        public Form1()
        {
            InitializeComponent();
            //  GetHostByName : 인터넷 DNS 서버에서 호스트 정보를 캐낸다. 빈 문자열을 호스트 이름으로 전달하면 이 메서드는 로컬 컴퓨터의 표준 호스트 이름을 검색한다.
            IPHostEntry hostIP = Dns.GetHostByName(Dns.GetHostName());
            // IPHostEntry.AddressList : 호스트와 연결된 IP 주소를 가져오거나 설정한다.
            // ToString() : 표준 숫자 형식 문자열로 변환한다.
            serverIP = hostIP.AddressList[0].ToString();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.txtPort.Text = 5555.ToString();
            this.txtIP.Text = serverIP.ToString();
            // .Enabled : 컨트롤이 사용자 상호 작용에 응답할 수 있으면 true이고, 그렇지 않으면 false이다.
            // Port와 IP 부분은 사용자가 설정할 수 없다.
            this.txtPort.Enabled = false;
            this.txtIP.Enabled = false;
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            dialogName = this.txtName.Text;
            // IsNullOrEmpty : 공백과 null 값을 체크해야 되는 경우에 사용하는 string 클래스 함수이다. 해당 예외처리는 필수값 체크 시 많이 사용되며, 필수값이 없을때 예외처리를 해주기 위해 자주 사용하는 함수이다.
            if (string.IsNullOrEmpty(dialogName))
            {
                MessageBox.Show("대화명을 입력하세요.");
                return;
            }
            if (string.IsNullOrEmpty(serverIP))
            {
                MessageBox.Show("주소를 입력하세요.");
                return;
            }
            if (string.IsNullOrEmpty(this.txtPort.Text))
            {
                MessageBox.Show("포트를 입력하세요.");
                return;
            }
            // .Parse() : 숫자 형식의 문자열을 정수로 변환할 수 있다.
            // Int32.Parse() : 32비트 부호 있는 정수 타입에 사용할 수 있다.
            serverPort = Int32.Parse(this.txtPort.Text);
            // isAlive 프로퍼티 : 현재 스레드의 실행 상태를 boolean 값으로 반환한다. 스레드가 시작된 경우 true를 반환하고 그렇지 않은 경우 false를 반환한다.
            isAlive = true;
            try
            {
                this.Echo();
                sendMessage("[" + dialogName + " 입장]");
            }
            // Exception : 예외처리 클래스이다. 모든 예외는 Exception클래스로부터 파생되므로 Exception클래스로 하나로 처리도 가능하다.
            // 원하는 예외는 처리 후 나머지 예외에 대하여는 동일하게 처리도 가능하다.
            // 주의할점 : switch문 같이 예외가 나왔을 때 그에 맞는 예외를 찾아가는게 아닌 무조건 위에 있는 catch문부터 찾아가기 때문에 원하는 예외가 있을 경우 위에 적어야 한다.
            catch (Exception)
            {
                // .Clear : 지우기 메서드
                this.txtName.Clear();
                this.isAlive = false;
            }
        }
        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)Keys.Enter == e.KeyChar)
            {
                string message = this.txtSend.Text;
                // .Trim : 현재 문자열의 앞쪽, 뒤쪽 공백을 모두 제거한 문자열을 반환한다.
                sendMessage("[" + dialogName + "] " + message.Trim());
                this.txtSend.Clear();
                // .SelectionStart : 텍스트 상자에서 선택한 텍스트의 시작 지점을 가져오거나 설정한다.
                this.txtSend.SelectionStart = 0;
            }
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            try
            {
                sendMessage("[" + dialogName + " 퇴장]");
                // .Close : 폼 닫기
                sr.Close();
                sw.Close();
                ns.Close();
            }
            catch { }
            finally
            {
                // Dispose : 바로 삭제가 필요한 리소스를 해제하는 함수
                this.Dispose();
            }
        }
        public void Echo()
        {
            try
            {
                // 클라이언트 연결
                client = new TcpClient(this.serverIP, this.serverPort);
                // GetStream(): 소켓에서 메시지를 가져오는 스트림
                ns = client.GetStream();
                // 메시지를 받아옴
                sr = new StreamReader(ns, Encoding.Default);
                // 메시지를 보냄
                sw = new StreamWriter(ns, Encoding.Default);
                Thread receiveThread = new Thread(new ThreadStart(run));
                // .IsBackground : 메인 프로세스가 종료될 때 Thread도 같이 종료됨.
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            // Exception : 예외처리 클래스이다. 모든 예외는 Exception클래스로부터 파생되므로 Exception클래스로 하나로 처리도 가능하다.
            catch (Exception e)
            {
                MessageBox.Show("서버 시작 실패");
                throw e;
            }
        }
        public void run()
        {
            string message = "start";
            try
            {
                // .Connected : Socket이 마지막으로 Send 또는 Receive 작업을 수행할 때 원격 호스트에 연결되었는지 여부를 나타내는 값을 가져온다.
                // && 논리연산 : true && true = true, true && true && false = true, false && false = false
                // != 관계 연산자 : a>b

                if (client.Connected && sr != null)
                    while ((message = sr.ReadLine()) != null)
                        AppendMessage(message);
            }
            catch (Exception) { MessageBox.Show("error"); }
        }
        public void AppendMessage(string message)
        {
            if (this.txtDialog != null && this.txtSend != null)
            {
                // .AppendText : 항상 스크롤이 BOTTOM 으로 가게된다.
                this.txtDialog.AppendText(message + "\r\n");
                this.txtDialog.Focus();
                // 글을 계속 입력받을 때 입력받은 마지막 줄에 포커스를 맞춰준다.
                this.txtDialog.ScrollToCaret();
            }
        }
        private void sendMessage(string message)
        {
            try
            {
                if (sw != null)
                {
                    // .WriteLine : 출력
                    sw.WriteLine(message);
                    // .Flush : 버퍼된 바이트를 모두 출력하여 버퍼를 비우하는 것을 명시하는 메소드이다.
                    sw.Flush();
                }
            }
            catch (Exception) { MessageBox.Show("전송실패"); }
        }
    }
}