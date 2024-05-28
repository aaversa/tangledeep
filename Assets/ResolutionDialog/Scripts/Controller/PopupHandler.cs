using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0649

namespace NewResolutionDialog.Scripts.Controller
{
    /// <summary>
    ///     Shows the popup at Start if the <see cref="ResolutionDialogStyle" /> is set to
    ///     <see cref="ResolutionDialogStyle.LaunchDialog" />
    /// </summary>
    /// <seealso cref="ResolutionDialogStyle.LaunchDialog" />
    /// <seealso cref="DefaultInputsHandler" />
    public class PopupHandler : MonoBehaviour
    {
        [SerializeField]
        private Settings settings;

        [SerializeField]
        private Canvas dialogCanvas;

        public Toggle showAtNextLaunchToggle;

        private void Start()
        {
            dialogCanvas.enabled = settings.dialogStyle == ResolutionDialogStyle.LaunchDialog;
        }

        private void Awake()
        {
            if (PlayerPrefs.GetInt("skip_checkbox") == 1)
            {
                SceneManager.LoadScene("Logos");
            }
        }

        public void OnToggleChanged()
        {
            PlayerPrefs.SetInt("skip_checkbox", showAtNextLaunchToggle.isOn ? 0 : 1);
        }
    }
}