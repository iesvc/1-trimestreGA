using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    // Declaración de los sliders y botones para controlar el volumen de música y efectos de sonido
    public Slider _musicSlider, _sfxSlider;
    public Button _musicButton, _sfxButton;
    public Sprite musicOn, sfxOn;  // Imágenes para indicar que la música y efectos están activados
    public Sprite musicMute, sfxMute; // Imágenes para indicar que la música y efectos están silenciados

    private void Start()
    {
        // Añade listeners para detectar cambios en los sliders y clics en los botones
        _musicSlider.onValueChanged.AddListener(OnMusicSliderValueChanged);
        _sfxSlider.onValueChanged.AddListener(OnSFXSliderValueChanged);
        _musicButton.onClick.AddListener(ToggleMusicButton);
        _sfxButton.onClick.AddListener(ToggleSFXButton);

        // Carga las configuraciones de volumen guardadas al iniciar
        LoadVolumeSettings();
    }

    // Método llamado cuando el valor del slider de música cambia
    private void OnMusicSliderValueChanged(float value)
    {
        // Cambia la imagen del botón según el valor del slider (mute o on)
        ChangeButtonImage(_musicButton, value == 0 ? musicMute : musicOn);
        MusicVolume(value); // Ajusta el volumen de la música
        SaveVolumeSettingsMusic(); // Guarda los ajustes de volumen
    }

    // Método llamado cuando el valor del slider de SFX cambia
    private void OnSFXSliderValueChanged(float value)
    {
        // Cambia la imagen del botón según el valor del slider (mute o on)
        ChangeButtonImage(_sfxButton, value == 0 ? sfxMute : sfxOn);
        SFXVolume(value); // Ajusta el volumen de los efectos de sonido
        SaveVolumeSettingsSFX(); // Guarda los ajustes de volumen
    }

    // Método para cambiar la imagen del botón
    private void ChangeButtonImage(Button button, Sprite sprite)
    {
        button.image.sprite = sprite; // Cambia la imagen del botón
    }

    // Método para alternar el estado del botón de música
    private void ToggleMusicButton()
    {
        ToggleButton(_musicButton, _musicSlider); // Alterna el botón y slider de música
        SaveVolumeSettingsMusic(); // Guarda los ajustes de volumen
    }

    // Método para alternar el estado del botón de SFX
    private void ToggleSFXButton()
    {
        ToggleButton(_sfxButton, _sfxSlider); // Alterna el botón y slider de SFX
        SaveVolumeSettingsSFX(); // Guarda los ajustes de volumen
    }

    // Método para alternar el estado de un botón y su slider asociado
    private void ToggleButton(Button button, Slider slider)
    {
        // Verifica si el botón está activado o silenciado
        if (button.image.sprite == musicOn || button.image.sprite == sfxOn)
        {
            // Cambia a estado silenciado
            button.image.sprite = button.image.sprite == musicOn ? musicMute : sfxMute;
            slider.value = 0f; // Establece el slider a 0
            if (button == _musicButton)
            {
                MusicVolume(0f); // Silencia la música
            }
            else
            {
                SFXVolume(0f); // Silencia los efectos de sonido
            }
        }
        else
        {
            // Cambia a estado activado
            button.image.sprite = button.image.sprite == musicMute ? musicOn : sfxOn;
            if (button == _musicButton)
            {
                MusicVolume(10f); // Restaura el volumen de la música
            }
            else
            {
                SFXVolume(10f); // Restaura el volumen de los efectos de sonido
            }
        }
    }

    // Método para ajustar el volumen de la música
    public void MusicVolume(float value)
    {
        _musicSlider.value = value; // Establece el valor del slider de música
        AudioManager.Instance.MusicVolume(value / 10f); // Ajusta el volumen en el AudioManager
    }

    // Método para ajustar el volumen de los efectos de sonido
    public void SFXVolume(float value)
    {
        _sfxSlider.value = value; // Establece el valor del slider de SFX
        AudioManager.Instance.SFXVolume(value / 10f); // Ajusta el volumen en el AudioManager
    }

    // Métodos para guardar los ajustes de volumen en PlayerPrefs
    private void SaveVolumeSettingsMusic()
    {
        PlayerPrefs.SetFloat("MusicVolume", _musicSlider.value); // Guarda el volumen de música
    }

    private void SaveVolumeSettingsSFX()
    {
        PlayerPrefs.SetFloat("SFXVolume", _sfxSlider.value); // Guarda el volumen de SFX
    }

    // Método para cargar los ajustes de volumen desde PlayerPrefs
    private void LoadVolumeSettings()
    {
        // Carga los valores guardados o establece un valor por defecto
        _musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 10f);
        _sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 10f);

        // Ajusta el volumen inicial de música y SFX
        MusicVolume(_musicSlider.value);
        SFXVolume(_sfxSlider.value);
    }
}
