using UnityEngine;
using UnityEngine.UI;

namespace DrinkitGame.UI
{
    [RequireComponent(typeof(Button))]
    public class BackToMainButton : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => UIRouter.Instance?.ShowMain());
        }
    }
}
