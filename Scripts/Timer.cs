using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//Класс для времени матча
public class Timer : MonoBehaviour
{   //Время матча в секундах
    static public int timeLeft = 60;
    public Text countdown;

    [PunRPC]
    void StartTimer(int time)
    {
        timeLeft = time;
        StartCoroutine("LoseTime");
    }
    void Update()
    {
        if (!ScoreManager.Instance.EndOfGame)
        {
            int min = timeLeft / 60;
            int sec = timeLeft - min * 60;
            countdown.text = (min.ToString() + " min " + sec.ToString() + " sec"); //Showing the Score on the Canvas
        }
        else
            countdown.text = "0 min 0 sec";
    }

    IEnumerator LoseTime()
    {
        while (timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft--;
            if (timeLeft <= 0)
            {
                //Установка флага конца игры
                ScoreManager.Instance.EndOfGame = true;
                //Показ панели результатов
                GameManager.Instance.ScoreBoard.SetActive(true);
            }
        }
    }
}