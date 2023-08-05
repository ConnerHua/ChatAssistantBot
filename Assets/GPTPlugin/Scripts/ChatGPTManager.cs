using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ChatGPTManager : MonoBehaviour
{
    //构造方法私有化，防止外部new对象
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

    //AI人设的提示词
    private string aiRolePrompt = "现在我希望你是数学老师";

    
    //发送给ChatGPT的数据
    [Serializable]
    public class PostData
    {
        //使用的ChatGPT模型
        public string model;
        //发送给ChatGPT的消息
        //如果发送的列表含有多条消息，则ChatGPT会根据上下文来回复
        public List<PostDataBody> messages;
    }

    [Serializable]
    public class PostDataBody
    {
        //说话的角色
        public string role;
        //说话的内容
        public string content;

        //构造方法
        public PostDataBody() { }

        public PostDataBody(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    //ChatGPT回复的数据
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
        //给AI设定的人设
        chatRecords.Add(new PostDataBody("system",aiRolePrompt));
    }

    /// <summary>
    /// 异步向ChatGPT发送消息（连续对话）
    /// </summary>
    /// <param name="meassage">询问ChatGPT的内容</param>
    /// <param name="callback">回调</param>
    public void ChatContinuously(string meassage, UnityAction<string> callback)
    {
        //缓存聊天记录
        chatRecords.Add(new PostDataBody("user", meassage));

        //构造要发送的数据
        PostData postData = new PostData()
        {
            //使用的ChatGPT模型
            model = chatGPTModel,

            messages = chatRecords
        };

        SendPostData(postData, callback);
    }
    
    /// <summary>
    /// 清空ChatGPT的聊天记录，并重新设置连续对话时AI的人设
    /// </summary>
    /// <param name="aiRolePrompt"></param>
    public void ClearChatRecordsAndSetAiRole(string aiRolePrompt)
    {
        //清空聊天记录
        chatRecords.Clear();

        //给AI设定人设
        chatRecords.Add(new PostDataBody("system", aiRolePrompt));
    }

    /// <summary>
    /// 异步向ChatGPT发送消息（不连续对话）
    /// </summary>
    /// <param name="meassage">询问ChatGPT的内容</param>
    /// <param name="callback">回调</param>
    /// <param name="aiRole">ChatGPT扮演的角色</param>
    public void ChatDiscontinuously(string meassage,UnityAction<string> callback,string aiRole="")
    {
        //构造要发送的数据
        PostData postData = new PostData()
        {
            //使用的ChatGPT模型
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

    //发送Chat请求
    IEnumerator SendPostDataCoroutine(PostData postData, UnityAction<string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(chatGPTUrl, "POST"))
        {
            //把传输的消息的对象转换为JSON格式的字符串。
            string jsonString = JsonUtility.ToJson(postData);

            //把Json格式的字符串转换为字节数组，以便进行网络传输
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonString);

            //设置要上传到远程服务器的主体数据。
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);

            //设置从远程服务器接收到的主体数据。
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            //设置HTTP网络请求的标头，表示这个网络请求的正文采用JSON格式进行编码
            request.SetRequestHeader("Content-Type", "application/json");

            //设置HTTP网络请求的标头，这里的写法是按照OpenAI官方要求来写的
            request.SetRequestHeader("Authorization", "Bearer " + chatGptApiKey);

            //等待ChatGPT回复
            yield return request.SendWebRequest();

            Debug.Log("request.responseCode : " + request.responseCode);
            //返回值200：成功，404：未找到，500：服务器内部错误
            if (request.responseCode == 200)
            {
                //获取ChatGPT回复的字符串，此时它是一个Json格式的字符串
                string respondedString = request.downloadHandler.text;
                Debug.Log("respondedString : " + respondedString);

                //将ChatGPT回复的JSON格式的字符串转换为指定的类的对象
                RespondedData respondedMessages = JsonUtility.FromJson<RespondedData>(respondedString);

                //有回复显示最新一条，索引为0
                if (respondedMessages != null && respondedMessages.choices.Count > 0)
                {
                    string respondedMessage = respondedMessages.choices[0].message.content;

                    callback?.Invoke(respondedMessage);
                }
            }
        }
    }
}