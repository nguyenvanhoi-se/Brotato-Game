using UnityEngine;

/// <summary>Phát nhạc nền và hiệu ứng ở các sự kiện chính của lượt chơi.</summary>
// Dịch vụ nhỏ dùng chung giữa các scene; giữ AudioSource riêng để hiệu ứng không ngắt nhạc.
public class GameAudio : MonoBehaviour
{
    // Chỉ giữ một bộ phát âm thanh qua các lần tải lại Gameplay.
    private static GameAudio instance;

    // Nhạc nền và nhạc thắng/thua dùng chung nguồn nhưng không phát cùng lúc.
    private AudioSource musicSource;
    // Hiệu ứng ngắn có thể chồng lên nhau khi đạn bắn liên tục.
    private AudioSource effectsSource;
    // Nguồn độc lập cho tia laser đang lặp.
    private AudioSource laserSource;

    // Clip được nạp một lần từ Assets/Resources/Audio.
    private AudioClip backgroundMusic;
    private AudioClip normalShot;
    private AudioClip explosiveShot;
    private AudioClip laserLoop;
    private AudioClip playerHit;
    private AudioClip playerDestroyed;
    private AudioClip asteroidDestroyed;
    private AudioClip starCollected;
    private AudioClip victory;
    private AudioClip gameOver;

    // Đặt lại static khi bắt đầu Play Mode, kể cả khi Editor tắt Domain Reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    // Tạo object bền qua scene và các AudioSource cần cho nhạc, hiệu ứng, tia laser.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = CreateSource("MusicSource", 0.16f);
        effectsSource = CreateSource("EffectsSource", 0.9f);
        laserSource = CreateSource("LaserSource", 0.85f);

        backgroundMusic = LoadClip("Music/background_music");
        normalShot = LoadClip("SFX/shoot_normal");
        explosiveShot = LoadClip("SFX/shoot_explosive");
        laserLoop = LoadClip("SFX/laser_loop");
        playerHit = LoadClip("SFX/player_hit");
        playerDestroyed = LoadClip("SFX/player_destroyed");
        asteroidDestroyed = LoadClip("SFX/asteroid_destroyed");
        starCollected = LoadClip("SFX/star_collect");
        victory = LoadClip("SFX/victory");
        gameOver = LoadClip("SFX/game_over");
    }

    // Bắt đầu nhạc nền lần đầu; khi qua màn giữ nguyên vị trí phát.
    public static void PlayGameplayMusic()
    {
        GameAudio audio = GetInstance();
        if (audio == null || audio.backgroundMusic == null) return;
        if (audio.musicSource.clip == audio.backgroundMusic && audio.musicSource.isPlaying) return;

        audio.musicSource.Stop();
        audio.musicSource.clip = audio.backgroundMusic;
        audio.musicSource.volume = 0.16f;
        audio.musicSource.loop = true;
        audio.musicSource.Play();
    }

    // Phát tiếng súng tại mỗi lần tạo viên đạn, cùng nhịp với cơ chế bắn tự động.
    public static void PlayShot(bool explosive)
    {
        GameAudio audio = GetInstance();
        if (audio == null) return;
        audio.PlayEffect(explosive ? audio.explosiveShot : audio.normalShot);
    }

    // Bật hoặc tắt tiếng tia cùng trạng thái của LineRenderer.
    public static void SetLaserActive(bool active)
    {
        GameAudio audio = GetInstance();
        if (audio == null || audio.laserLoop == null) return;
        if (active)
        {
            if (audio.laserSource.clip != audio.laserLoop || !audio.laserSource.isPlaying)
            {
                audio.laserSource.clip = audio.laserLoop;
                audio.laserSource.loop = true;
                audio.laserSource.Play();
            }
        }
        else if (audio.laserSource.isPlaying)
        {
            audio.laserSource.Stop();
        }
    }

    // Tiếng va chạm chỉ được gọi sau khi kiểm tra bất tử và thực sự trừ mạng.
    public static void PlayPlayerHit()
    {
        GameAudio audio = GetInstance();
        if (audio != null) audio.PlayEffect(audio.playerHit);
    }

    // Phát vụ nổ khi lượt chơi hết mạng; AudioSource còn sống sau khi Player bị tắt.
    public static void PlayPlayerDestroyed()
    {
        GameAudio audio = GetInstance();
        if (audio != null) audio.PlayEffect(audio.playerDestroyed);
    }

    // Tiếng thiên thạch vỡ phát cả khi bị bắn hạ lẫn va chạm với Player.
    public static void PlayAsteroidDestroyed()
    {
        GameAudio audio = GetInstance();
        if (audio != null) audio.PlayEffect(audio.asteroidDestroyed);
    }

    // Báo nhận Star sau khi GameManager xác nhận còn được phép nhặt.
    public static void PlayStarCollected()
    {
        GameAudio audio = GetInstance();
        if (audio != null) audio.PlayEffect(audio.starCollected);
    }

    // Thay nhạc nền bằng một đoạn kết quả thắng hoặc thua.
    public static void PlayResult(bool won)
    {
        GameAudio audio = GetInstance();
        if (audio == null) return;
        audio.SetLaser(false);
        AudioClip result = won ? audio.victory : audio.gameOver;
        if (result == null) return;

        audio.musicSource.Stop();
        audio.musicSource.clip = result;
        audio.musicSource.volume = 0.9f;
        audio.musicSource.loop = false;
        audio.musicSource.Play();
    }

    // Dừng mọi âm thanh khi người chơi thoát về MainMenu.
    public static void StopAll()
    {
        if (instance == null) return;
        AudioListener.pause = false;
        instance.musicSource.Stop();
        instance.effectsSource.Stop();
        instance.SetLaser(false);
    }

    // Tạo AudioSource 2D để âm lượng không phụ thuộc vị trí phi thuyền.
    private AudioSource CreateSource(string sourceName, float sourceVolume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = sourceVolume;
        return source;
    }

    // Tải clip theo đường dẫn Resources và báo sớm nếu asset thiếu.
    private static AudioClip LoadClip(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + resourcePath);
        if (clip == null) Debug.LogWarning("Không tìm thấy âm thanh Resources/Audio/" + resourcePath);
        return clip;
    }

    // Lấy hoặc dựng service; các lần gọi sau dùng lại AudioSource hiện có.
    private static GameAudio GetInstance()
    {
        if (instance == null)
        {
            GameObject audioObject = new GameObject("GameAudio");
            instance = audioObject.AddComponent<GameAudio>();
        }
        return instance;
    }

    // Dùng PlayOneShot để tiếng bắn/va chạm mới không cắt hiệu ứng đang phát.
    private void PlayEffect(AudioClip clip)
    {
        if (clip != null) effectsSource.PlayOneShot(clip);
    }

    // Dừng riêng tia laser để nhạc và các hiệu ứng ngắn vẫn tiếp tục.
    private void SetLaser(bool active)
    {
        if (active)
        {
            if (laserLoop != null && !laserSource.isPlaying)
            {
                laserSource.clip = laserLoop;
                laserSource.loop = true;
                laserSource.Play();
            }
        }
        else if (laserSource.isPlaying)
        {
            laserSource.Stop();
        }
    }
}
