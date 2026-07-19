using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using UnityEngine.UI;

public class Saver : MonoBehaviour
{
    public TextBeginner TB;
    public GameObject Sav;
    [System.Serializable]
    public class Say
    {
        public List<string> say = new List<string>();
        public List<string> arrayString = new List<string>();
        public int tx;
        public bool inDialogue;
        public string interlocutors;
    }
    public void Save()
    {
        Say say = new Say();
        //say.say = TB.dialogueLines;
        //say.arrayString = TB.currentLineParts;
        //say.tx = TB.currentLineIndex;
        //say.inDialogue = TB.isInDialogue;
        //say.interlocutors = TB.currentInterlocutorName;
        if (!Directory.Exists(Application.dataPath + "/saves"))
        {
            Directory.CreateDirectory(Application.dataPath + "/saves");
        }
        FileStream fs = new FileStream(Application.dataPath + "/saves/save.sv", FileMode.Create);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, say);
        fs.Close();
        Debug.Log("1");
    }
    public void Load()
    {
        //    if (File.Exists(Application.dataPath + "/saves/save.sv"))
        //    {
        //        FileStream fs = new FileStream(Application.dataPath + "/saves/save.sv", FileMode.Open);
        //        BinaryFormatter bf = new BinaryFormatter();
        //        try
        //        {
        //            Say say = (Say)bf.Deserialize(fs);
        //            TB.dialogueLines = say.say;
        //            TB.currentLineParts = say.arrayString;
        //            TB.currentLineIndex = say.tx;
        //            TB.isInDialogue = say.inDialogue;
        //            if (say.inDialogue)
        //            {
        //                for (int i = 0; i < TB.currentInterlocutorName.Length; i++)
        //                {
        //                    if (TB.currentInterlocutorName[i].name == say.interlocutors)
        //                    {
        //                        TB.currentInterlocutorName[i].SetActive(true);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                for (int i = 0; i < TB.currentInterlocutorName.Length; i++)
        //                {
        //                    if (TB.currentInterlocutorName[i].name == say.interlocutors)
        //                    {
        //                        TB.currentInterlocutorName[i].SetActive(false);
        //                    }
        //                }
        //            }
        //        }
        //        catch (System.Exception e)
        //        {
        //            Debug.Log(e.Message);
        //        }
        //        finally
        //        {
        //            fs.Close();
        //        }
        //        //TB.Load();
        //    }
        //    else
        //    {
        //        Application.Quit();
        //    }
    }
    public void Start()
    {
        if (PlayerPrefs.GetInt("Continue") == 1)
        {
            Load();
        }
        PlayerPrefs.SetInt("Continue", 0);
    }
}
