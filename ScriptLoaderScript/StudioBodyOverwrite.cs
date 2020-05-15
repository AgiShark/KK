// #name Body Overwrite
// #author jim60105 
// #desc Overwrite all the characters' bodies in Studio. Use KeyCode.O.
// #proc_filter CharaStudio.exe
// v20.05.15.1
// v1.0.0

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BodyOverwrite {
    private static GameObject gameObject;
    public static void Main() {
        gameObject = new GameObject();
        gameObject.AddComponent<BO>();
    }

    public static void Unload() {
        // Unload and unpatch everything before reloading the script
        Object.Destroy(gameObject);
        gameObject = null;
    }

    class BO : MonoBehaviour {
        void Awake() {
            DontDestroyOnLoad(this);
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.O)) {  //��L��ť
                ChangeAll();
            }
        }

        void ChangeAll() {
            List<ChaControl> charList = new List<ChaControl>();

            charList = Studio.Studio.Instance.dicInfo.Values.OfType<Studio.OCIChar>().Select(x => x.charInfo).ToList();
            foreach (ChaControl chaCtrl in charList) {
                ChaFileCustom chaFileCustom = chaCtrl.chaFile.custom;
                chaFileCustom.body.shapeValueBody[4] = 1.000f;   //�ݤj�p
                chaFileCustom.body.shapeValueBody[5] = 0.008f;   //�ݤW�U��m
                chaFileCustom.body.shapeValueBody[6] = 0.733f;   //�ݥ��k�}
                chaFileCustom.body.shapeValueBody[7] = 0.696f;   //�ݥ��k��m
                chaFileCustom.body.shapeValueBody[8] = 0.486f;   //�ݤW�U����
                chaFileCustom.body.shapeValueBody[9] = 0.303f;   //�ݦy
                chaFileCustom.body.shapeValueBody[10] = 1.000f;  //�ݧΪ�
                chaFileCustom.body.bustSoftness = 1.100f;        //�ݬX
                chaFileCustom.body.bustWeight = 0.260f;          //�ݭ�
                chaFileCustom.body.shapeValueBody[11] = 0.016f;  //�Ž���
                chaFileCustom.body.shapeValueBody[12] = 0.506f;  //�ŭ���
                chaFileCustom.body.shapeValueBody[13] = 0.620f;  //�ŭ���
                chaFileCustom.body.areolaSize = 1.022f;          //�Ž��j

                chaFileCustom.body.skinMainColor = new Color(0.72f, 0.61f, 0.54f, 1f);  //����
                chaFileCustom.body.skinSubColor = new Color(0.44f, 0.38f, 0.29f, 1f);   //������
                chaFileCustom.body.skinGlossPower = 0.203f; //�ֽ�����
                chaFileCustom.body.nipColor = new Color(0.52f, 0.45f, 0.44f, 0.69f);    //�ŭ���
                chaFileCustom.body.nipGlossPower = 0.210f;  //�ŭ�����

                chaCtrl.Reload(true, true, true);
            }
        }
    }
}