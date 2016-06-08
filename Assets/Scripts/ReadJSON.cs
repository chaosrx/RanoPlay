using UnityEngine;
using System.Collections;
using MiniJSON;
using System.Collections.Generic;
 
public class ReadJSON : MonoBehaviour {
 
	private string path = "https://app.ranoplay.com/app/test.json";
 
	// Use this for initialization
	 IEnumerator Start () {
	
		using(WWW www = new WWW(path)){
 
			yield return www;
 
			if(!string.IsNullOrEmpty(www.error)){
 
				Debug.LogError("www Error:" + www.error);
				yield break;
 
			}
 
			Debug.Log(www.text);
            var jsonDict = Json.Deserialize(www.text) as Dictionary<string,object>;
			Debug.Log(jsonDict["message"]);
			Debug.Log(jsonDict["test2"]);
			Debug.Log(jsonDict["test3"]);
 
            var jsonDict2 = Json.Deserialize(www.text) as Dictionary<string,object>;
			Debug.Log(jsonDict2["message"]);
			Debug.Log(jsonDict2["test2"]);
			Debug.Log(jsonDict2["test3"]);
			}
 
			
		}
}
	
