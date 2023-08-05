using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ChatGPTManager : MonoBehaviour
{
    //���췽��˽�л�����ֹ�ⲿnew����
    private ChatGPTManager() { }

    private static ChatGPTManager instance;
    public static ChatGPTManager Instance
    {
        get
        {
            if (instance == null)
            { 
                instance = FindObjectOfType<ChatGPTManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ChatGPTManager");
                    instance = go.AddComponent<ChatGPTManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private string chatGPTUrl = "https://api.openai.com/v1/chat/completions";

    private string chatGPTModel = "gpt-3.5-turbo";

    private string chatGptApiKey = "";

    //AI�������ʾ��
    private string aiRolePrompt = "������ϣ��������ѧ��ʦ";

    
    //���͸�ChatGPT������
    [Serializable]
    public class PostData
    {
        //ʹ�õ�ChatGPTģ��
        public string model;
        //���͸�ChatGPT����Ϣ
        //������͵��б��ж�����Ϣ����ChatGPT��������������ظ�
        public List<PostDataBody> messages;
    }

    [Serializable]
    public class PostDataBody
    {
        //˵���Ľ�ɫ
        public string role;
        //˵��������
        public string content;

        //���췽��
        public PostDataBody() { }

        public PostDataBody(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    //ChatGPT�ظ�������
    [Serializable]
    public class RespondedData
    {
        public string id;
        public string created;
        public string model;
        public List<RespondedChoice> choices;
    }

    [Serializable]
    public class RespondedChoice
    {
        public RespondedDataBody message;
        public string finish_reason;
        public int index;
    }

    [Serializable]
    public class RespondedDataBody
    {
        public string role;
        public string content;
    }

    public List<PostDataBody> chatRecords = new List<PostDataBody>();

    private void Awake()
    {
        //��AI�趨������
        chatRecords.Add(new PostDataBody("system",aiRolePrompt));
    }

    /// <summary>
    /// �첽��ChatGPT������Ϣ�������Ի���
    /// </summary>
    /// <param name="meassage">ѯ��ChatGPT������</param>
    /// <param name="callback">�ص�</param>
    public void ChatContinuously(string meassage, UnityAction<string> callback)
    {
        //���������¼
        chatRecords.Add(new PostDataBody("user", meassage));

        //����Ҫ���͵�����
        PostData postData = new PostData()
        {
            //ʹ�õ�ChatGPTģ��
            model = chatGPTModel,

            messages = chatRecords
        };

        SendPostData(postData, callback);
    }
    
    /// <summary>
    /// ���ChatGPT�������¼�����������������Ի�ʱAI������
    /// </summary>
    /// <param name="aiRolePrompt"></param>
    public void ClearChatRecordsAndSetAiRole(string aiRolePrompt)
    {
        //��������¼
        chatRecords.Clear();

        //��AI�趨����
        chatRecords.Add(new PostDataBody("system", aiRolePrompt));
    }

    /// <summary>
    /// �첽��ChatGPT������Ϣ���������Ի���
    /// </summary>
    /// <param name="meassage">ѯ��ChatGPT������</param>
    /// <param name="callback">�ص�</param>
    /// <param name="aiRole">ChatGPT���ݵĽ�ɫ</param>
    public void ChatDiscontinuously(string meassage,UnityAction<string> callback,string aiRole="")
    {
        //����Ҫ���͵�����
        PostData postData = new PostData()
        {
            //ʹ�õ�ChatGPTģ��
            model = chatGPTModel,

            messages = new List<PostDataBody>()
            {
                new PostDataBody("system",aiRole),
                new PostDataBody("user",meassage)
            }
        };

        SendPostData(postData, callback);
    }

   
    public void SendPostData(PostData post,UnityAction<string> callback)
    {
        StartCoroutine(SendPostDataCoroutine(post, callback));
    }

    //����Chat����
    IEnumerator SendPostDataCoroutine(PostData postData, UnityAction<string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(chatGPTUrl, "POST"))
        {
            //�Ѵ������Ϣ�Ķ���ת��ΪJSON��ʽ���ַ�����
            string jsonString = JsonUtility.ToJson(postData);

            //��Json��ʽ���ַ���ת��Ϊ�ֽ����飬�Ա�������紫��
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonString);

            //����Ҫ�ϴ���Զ�̷��������������ݡ�
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);

            //���ô�Զ�̷��������յ����������ݡ�
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            //����HTTP��������ı�ͷ����ʾ���������������Ĳ���JSON��ʽ���б���
            request.SetRequestHeader("Content-Type", "application/json");

            //����HTTP��������ı�ͷ�������д���ǰ���OpenAI�ٷ�Ҫ����д��
            request.SetRequestHeader("Authorization", "Bearer " + chatGptApiKey);

            //�ȴ�ChatGPT�ظ�
            yield return request.SendWebRequest();

            Debug.Log("request.responseCode : " + request.responseCode);
            //����ֵ200���ɹ���404��δ�ҵ���500���������ڲ�����
            if (request.responseCode == 200)
            {
                //��ȡChatGPT�ظ����ַ�������ʱ����һ��Json��ʽ���ַ���
                string respondedString = request.downloadHandler.text;
                Debug.Log("respondedString : " + respondedString);

                //��ChatGPT�ظ���JSON��ʽ���ַ���ת��Ϊָ������Ķ���
                RespondedData respondedMessages = JsonUtility.FromJson<RespondedData>(respondedString);

                //�лظ���ʾ����һ��������Ϊ0
                if (respondedMessages != null && respondedMessages.choices.Count > 0)
                {
                    string respondedMessage = respondedMessages.choices[0].message.content;

                    callback?.Invoke(respondedMessage);
                }
            }
        }
    }
}