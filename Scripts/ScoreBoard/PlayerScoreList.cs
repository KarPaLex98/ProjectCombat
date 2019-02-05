using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//Класс для отображения таблицы результатов
public class PlayerScoreList : MonoBehaviour {

	public GameObject playerScoreEntryPrefab;

	ScoreManager scoreManager;

	void Start () {
		scoreManager = GameObject.FindObjectOfType<ScoreManager>();
	}
	
	void Update () {
		if(scoreManager == null) {
			Debug.LogError("You forgot to add the score manager component to a game object!");
			return;
		}

		while(this.transform.childCount > 0) {
			Transform c = this.transform.GetChild(0);
			c.SetParent(null);
			Destroy (c.gameObject);
		}

		string[] names = scoreManager.GetPlayerNames("kills");
		//Перебор всех  игроков и создание для каждого своего раздела в таблице результатов
		foreach(string name in names) {
			GameObject go = (GameObject)Instantiate(playerScoreEntryPrefab);
			go.transform.SetParent(this.transform);
			go.transform.Find ("Username").GetComponent<Text>().text = name;
			go.transform.Find ("Kills").GetComponent<Text>().text = scoreManager.GetScore(name, "kills").ToString();
			go.transform.Find ("Deaths").GetComponent<Text>().text = scoreManager.GetScore(name, "deaths").ToString();
		}
	}
}
