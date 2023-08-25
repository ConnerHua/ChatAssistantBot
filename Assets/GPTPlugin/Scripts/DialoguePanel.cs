using CognitiveServicesTTS;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : MonoBehaviour
{
    public Text speakerName;
    public Text content;
    public InputField inputField;
    public Button sendButton;

    public float verbatimIntervalTime = 0.1f;
    //逐字显示的携程
    private Coroutine verbatimCoroutine;

    public SpeechManager speech;
    public bool isSpeechOn = true;

    void Start()
    {
        sendButton.onClick.AddListener(Send);
    }

    private void Send()
    {
        //清空上一次的对话内容
        ShowDialogue("");

        //防止连续点击
        sendButton.interactable = false;

        ChatGPTManager.Instance.ChatContinuously(inputField.text, (content) => 
        {
            ShowDialogue("小助理", content);

            SpeechPlayback(content);

            sendButton.interactable = true;
        });

        //清空输入框
        inputField.text = "";
    }

    private void SpeechPlayback(string content)
    {
        if (speech.isReady && isSpeechOn)
        {
            speech.voiceName = VoiceName.zhCNXiaoxiaoNeural;
            speech.SpeakWithRESTAPI(content);
        }
    }

    public void ShowDialogue(string speakerName, string content,bool isVerbatim = true) 
    {
        this.speakerName.text = speakerName;

        if (!isVerbatim)
        {
            this.content.text = content;
        }
        else
        {
            //清空上一次的内容
            this.content.text = "";

            //关闭上一次的携程
            if(verbatimCoroutine!=null)
                StopCoroutine(verbatimCoroutine);

            verbatimCoroutine = StartCoroutine(VebatimCoroutine(content));
        }
    }

    public void ShowDialogue( string content, bool isVerbatim = true)
    {
        ShowDialogue("", content, isVerbatim);
    }
    
    IEnumerator VebatimCoroutine(string content)
    {
        //暂时等待1帧，用于跳过外部，把协程记录起来。
        yield return null;

        int letter = 0;
        
        while (letter < content.Length) 
        {
            if (content[letter] == '<')
            {
                string remainingString = content.Substring(letter);

                int startTagLength = remainingString.IndexOf('>') + 1;

                if (startTagLength != 0)
                {
                    string startTag = remainingString.Substring(0, startTagLength);

                    if (startTag == "<b>")
                    {
                        string endTag = "</b>";

                        int endTagIndex = remainingString.Substring(startTagLength).IndexOf("</b>");

                        if(endTagIndex != -1)
                        {
                            string tempString = remainingString.Substring(startTagLength);

                            string stringContent  =  tempString.Substring(0, endTagIndex);

                            this.content.text += $"<b>{stringContent}</b>";

                            letter += startTagLength + stringContent.Length + endTag.Length;

                            yield return new WaitForSeconds(verbatimIntervalTime);
                            continue;
                        }
                    }
                    else if (startTag == "<i>")
                    {
                        string endTag = "</i>";

                        int endTagIndex = remainingString.Substring(startTagLength).IndexOf("</i>");

                        if (endTagIndex != -1)
                        {
                            string tempString = remainingString.Substring(startTagLength);

                            string stringContent = tempString.Substring(0, endTagIndex);

                            this.content.text += $"<i>{stringContent}</i>";
                            letter += startTagLength + stringContent.Length + endTag.Length;
                            yield return new WaitForSeconds(verbatimIntervalTime);
                            continue;
                        }
                    }
                    else if (startTag.StartsWith("<size")&&startTag.EndsWith(">"))
                    {
                        string endTag = "</size>";

                        string value = startTag.Substring(startTag.IndexOf('=') + 1).TrimEnd('>');

                        string tempString = remainingString.Substring(startTagLength);

                        int endTagIndex = tempString.IndexOf(endTag);

                        if(endTagIndex != -1)
                        {
                            string stringContent = tempString.Substring(0, endTagIndex);

                            this.content.text += $"<size={value}>{stringContent}</size>";
                            letter += startTagLength + stringContent.Length + endTag.Length;
                            yield return new WaitForSeconds(verbatimIntervalTime);
                            continue;
                        }
                    }
                    else if (startTag.StartsWith("<color") && startTag.EndsWith(">"))
                    {
                        string endTag = "</color>";

                        string value = startTag.Substring(startTag.IndexOf('=') + 1).TrimEnd('>');

                        string tempString = remainingString.Substring(startTagLength);

                        int endTagIndex = tempString.IndexOf(endTag);

                        if (endTagIndex != -1)
                        {
                            string stringContent = tempString.Substring(0, endTagIndex);

                            this.content.text += $"<color={value}>{stringContent}</color>";
                            letter += startTagLength + stringContent.Length + endTag.Length;
                            yield return new WaitForSeconds(verbatimIntervalTime);
                            continue;
                        }
                    }
                    else
                    {
                        this.content.text += content[letter];
                        letter++;
                        yield return new WaitForSeconds(verbatimIntervalTime);
                        continue;
                    }
                }
            }

            this.content.text += content[letter];
            letter++;
            yield return new WaitForSeconds(verbatimIntervalTime);
        }
    }
}
