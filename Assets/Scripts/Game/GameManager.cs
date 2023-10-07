using System.Collections;
using Behavior;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public Text timerText;
        private float startTime;
        private bool gameEnded;
        public Transform bossRoom;
        public Transform lookat;
        private bool isFinalBattle = false;
        public float ElapsedTime;
        public float RemainingTime;
        public AudioSource BGM;

        private void Start()
        {
            gameEnded = false;
            ElapsedTime = 0f;
            // 获取当前场景加载的时间
            startTime = Time.timeSinceLevelLoad;
            if(!lookat) lookat = GameObject.Find("SM_Prop_Table_04").transform;
        }

        private void Update()
        {
            if (!gameEnded)
            {
                // 计算从场景加载开始经过的时间
                ElapsedTime = Time.timeSinceLevelLoad - startTime;

                // 计算剩余时间
                RemainingTime = 300 - ElapsedTime;

                // 更新倒计时文本
                UpdateTimerText(RemainingTime);

                if (ElapsedTime >= 240) // 240秒 = 4分钟
                {
                    if (!isFinalBattle)
                    {
                        // 触发决战事件，将玩家传送至指定位置
                        PlayerController.Instance.ShowPlayerHUD("The decisive battle is coming! Hold on!");
                        StartCoroutine(TeleportPlayerToFloorLarge());
                    }
                    
                    if (ElapsedTime >= 300) // 300秒 = 5分钟
                    {
                        ElapsedTime = -1f;
                        // 游戏胜利，加载WinScene场景
                        LoadWinScene();
                    }
                }
            }
        }

        private void UpdateTimerText(float remainingTime)
        {
            int minutes = Mathf.FloorToInt((remainingTime - 60) / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            if (remainingTime >= 60)
            {
                timerText.text = "Countdown to the decisive battle: " + minutes.ToString("00") + " Min " + seconds.ToString("00") + " s";
            }
            else
            {
                timerText.text = "Hold On Final: " + seconds.ToString("00") + " s";
            }
        }

        private IEnumerator TeleportPlayerToFloorLarge()
        {
            isFinalBattle = true;
            
            ParticleSystem transfer = Resources.Load<ParticleSystem>("Liberate_04.1_Darkness");
            if (transfer == null) Debug.LogError("NO transfer");
            var transferi = new ParticleSystem[8];
            PlayerController playerController = PlayerController.Instance;
            for (int x = 0; x < 8; ++x)
            {
                transferi[x] = Instantiate(transfer, playerController.transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(2f);
            for (int x = 0; x < 8; ++x)
            {
                transferi[x].Stop();
                Destroy(transferi[x].gameObject);
            }
            playerController.transform.position = bossRoom.position + Vector3.up * 2f;
            // PlayerController.Instance.transform.forward =lookat.position - PlayerController.Instance.transform.position;
            Vector3 targetDirection = lookat.transform.position - playerController.transform.position;
            targetDirection.y = 0f; // 将Y轴分量置零，以确保只在水平面上旋转
            float maxRotationAngle = 360f; // 调整最大旋转角度
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            // 旋转到目标方向
            while (Quaternion.Angle(playerController.transform.rotation, targetRotation) > 0.1f)
            {
                // 计算旋转步长
                float step = maxRotationAngle * Time.deltaTime;

                // 使用RotateTowards旋转
                playerController.transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);
                yield return null;
            }
            
            
            if(!BGM) BGM = GameObject.Find("BGM").GetComponent<AudioSource>();
            BGM.clip = Resources.Load<AudioClip>("Music/沙场");
            BGM.Play();
        }

        private void LoadWinScene()
        {
            // 游戏胜利，加载WinScene场景
            SceneManager.LoadScene("WinScene");
            gameEnded = true;
        }
    }
}
