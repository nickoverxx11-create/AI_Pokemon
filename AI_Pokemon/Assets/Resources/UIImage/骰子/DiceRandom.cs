using System.Collections;
using toio.Samples.Sample_Sensor;
using UnityEngine;
using UnityEngine.UI;

public class DiceRandom : MonoBehaviour
{
    public Button diceBtn;
    public Image diceDis;
    public Sprite[] dicesprs;

    public static int moveIndex = 0;//移动过的格子数
    private void Start()
    {
        // 初始状态设置为完全透明
        diceDis.color = new Color(1, 1, 1, 0);
        // 绑定按钮点击事件
        diceBtn.onClick.AddListener(StartRoll);
    }

    public void StartRoll()
    {
        diceBtn.interactable = false;
        StopAllCoroutines();
        Time.timeScale = 0f;//linshi
        StartCoroutine(RollDiceCoroutine());
    }

    private IEnumerator RollDiceCoroutine()
    {
        // 初始淡入
        float fadeDuration = 0.5f;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            diceDis.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
        diceDis.color = Color.white;

        // 设置初始随机精灵
        diceDis.sprite = dicesprs[Random.Range(0, dicesprs.Length)];

        // 骰子滚动时间
        float rollDuration = 2f;
        float switchInterval = 0.2f;
        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            // 淡出当前精灵
            timer = 0f;
            while (timer < switchInterval / 2)
            {
                timer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1, 0, timer / (switchInterval / 2));
                diceDis.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            // 切换精灵
            diceDis.sprite = dicesprs[Random.Range(0, dicesprs.Length)];

            // 淡入新精灵
            timer = 0f;
            while (timer < switchInterval / 2)
            {
                timer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0, 1, timer / (switchInterval / 2));
                diceDis.color = new Color(1, 1, 1, alpha);
                yield return null;
            }

            elapsed += switchInterval;
        }

        // 显示最终结果
        int finalIndex = Random.Range(0, dicesprs.Length);
        diceDis.sprite = dicesprs[finalIndex];
        diceDis.color = Color.white;
        Debug.Log("点数为：" + (finalIndex + 1));

        diceBtn.interactable = true;
        //Sample_Sensor.Instance.CubeMoveByRoll(finalIndex + 1);
    }
}