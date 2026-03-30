# Seek-And-Destroy
<img width="49%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/0154957b-0387-47b2-a31a-66ba03a3929b" />
<img width="49%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/71521378-9db9-437c-9e4a-e53e928077f0" />
<div align="center">
  <b><게임 이미지></b>
</div>

- Photon Pun2 패키지를 활용한 PC 온라인 슈팅 게임 입니다

- 유료 에셋 사용으로 인해 소스 코드만 등록된 점을 밝힙니다

## 1. 게임소개
- 소개
  - "**찾아내고 쏜다**"를 핵심 컨셉으로 한 1vs1 PC 온라인 슈팅 게임입니다
  - 플레이어는 상대에게 시야를 들키지 않은 채 위치를 파악하고, 먼저 적을 처치해 점수를 획득해야 합니다
  - 지형지물을 활용한 은신과 소리를 통한 위치 추적이 핵심 플레이 요소로 작용합니다

- 개발 일자: 2026.01 ~ 2025.03 (약 2개월 소요)
- 개발 인원: 1인
- 개발 환경: C#, PUN 2, Unity 6000.3.0f1 LTS

## 2. 플레이 영상
https://www.youtube.com/watch?v=GYJoj1vSZJM

## 3. 게임 빌드
https://drive.google.com/file/d/1wy4DfbY1S3tkfV85JXWjRsx2NG9XSJVx/view?usp=drive_link

## 4. 기술 문서
https://drive.google.com/file/d/1lnGh7ddnWrCNTFCk0GpFFtzKv-hwJ7f7/view?usp=drive_link

## 5. 핵심 기능 소개
  
### 1. Photon 서버 구현
<details open>
  <summary>Room 생성 코드 일부</summary>
  
```csharp
//RoomNetworkManager.cs
private void CreateRoomInternal()
{
    string roomCode = RoomCodeService.GenerateRoomCode();
    currentRoomCode = roomCode;

    // 방 코드로만 진입이 가능하도록
    RoomOptions roomOptions = new RoomOptions();
    roomOptions.MaxPlayers = FIXED_MAX_PLAYERS;
    roomOptions.IsOpen = true;
    roomOptions.IsVisible = false;

    // 디폴트 값 세팅
    Hashtable roomProperties = new Hashtable();
    roomProperties[NetKeys.RoomKey.PLAY] = false;
    roomProperties[NetKeys.RoomKey.MAP_ID] = defaultMapId;

    roomOptions.CustomRoomProperties = roomProperties;
    PhotonNetwork.CreateRoom(roomCode, roomOptions);
}
```
</details>

- **PUN 2** 기반 멀티플레이 구조를 활용하여 로비 상태 동기화부터 게임 시작까지의 흐름을 구현했습니다.

- **Custom Properties**와 **RPC**를 활용하여 방 상태와 플레이어가 선택한 캐릭터, 무기 정보를 각 클라이언트 간 일관되게 공유되도록 구성했습니다.

- 인게임에서는 RPC 기반 동기화를 통해 총 발사, 사망, 피격 등 각 플레이어 상태가 모든 클라이언트에 동일하게 적용되도록 구현했습니다.

- 네트워크 관련 스크립트를 분리하여 로비 관리, 상태 집계, 씬 전환, 네트워크 키 관리가 각각 독립된 역할로 동작하도록 구성했습니다.

### 2. 시야 시스템 (FOV)
<details open>
  <summary>플레이어 시야시스템 코드 일부</summary>

```csharp
//PlayerFovController.cs Tick 함수
public void Tick()
{
    if (!isInitialized) return;

    float now = Time.time;
    Vector3 aimDirection = GetAimDirection();

    ScanAndReportVisibleTargets(aimDirection, now);

    if (visibilityController != null)
    {
        visibilityController.Tick(now);
    }
}

public void LateTick()
{
    if (!isInitialized) return;
    Vector3 aimDirection = GetAimDirection();

    DrawConeFieldOfViewMesh(aimDirection);

    if (useCircleFov)
    {
        DrawCircleFieldOfViewMesh();
    }

    else
    {
        ClearMesh(circleFovMesh);
    }
}
```
</details>

- 플레이어 위치, 조준 방향을 기준으로 시야 범위를 계산하고, 오브젝트 렌더러를 런타임에서 제어하는 FOV 시스템을 구현했습니다.

- **Raycast, Shader Graph, Camera Render, Render Texture** 등을 조합하여 플레이어 시야 표현을 구성했습니다.

- 플레이어 시야 생성, 오브젝트 시야 판정, FOV Mesh 생성 관련 스크립트를 분리하여 유지보수성을 높이도록 구성했습니다.

### 3. Sound / Effect / 오브젝트 풀링
<details open>
  <summary>사운드 재생 코드 일부</summary>

```csharp
//SoundManager.cs 사운드 재생 함수
public void Play3DSound(SoundSO soundSO, Vector3 soundPoint, float volumeMultiplier = 1f)
{
    if (soundSO == null) return;

    AudioSource audioSource = GetPool(sfx3DPool);
    SoundSourceSettingSO setting = GetSetting(soundSO.soundType);
    ApplySetting(audioSource, setting);

    audioSource.volume *= volumeMultiplier;

    audioSource.clip = soundSO.soundClip;
    audioSource.transform.position = soundPoint;
    audioSource.Play();
}
```
</details>

- 런타임 중 반복적으로 발생하는 사운드와 이펙트를 **오브젝트 풀링(Object Pooling)** 방식으로 관리하여 오버헤드를 줄이는 구조로 구성했습니다.

- SoundSourceSettingSO를 제작하여 재생할 사운드 Enum에 따라 설정값이 자동으로 적용되도록 구성했으며, 이를 기반으로 사운드 오브젝트를 재사용할 수 있도록 구현했습니다.

### 4. 플레이어 초기화 및 캐릭터 반영
<details open>
  <summary>캐릭터 초기화 코드 일부</summary>

```csharp
//PlayerInitializeController.cs 캐릭터 초기화 함수 일부
private bool TryInitialize()
{
    ...
    if (!TryInitializeCharacter()) return false;
    if (!TryInitializeWeapon()) return false;
    if (!InitializeVisibilityTargets()) return false;

    InitializeStats();
    InitializeAnimation();

    if (localSetupController != null)
    {
        localSetupController.SetupLocalPlayer();
    }

    if (roundLifecycleController != null)
    {
        roundLifecycleController.BindRoundEvents();
        roundLifecycleController.ApplyCurrentPhaseLock();
        roundLifecycleController.TryResetCurrentRound();
    }

    isReady = true;
    return true;
}
```
</details>

- 각 클라이언트가 로비에서 선택한 캐릭터, 무기, 맵 정보가 게임 씬 초기화 단계에서 자신의 플레이어 오브젝트 프리팹에 반영되도록 구현했습니다.

- 캐릭터 초기화 시 공통 AnimatorController를 기반으로, 선택한 캐릭터와 무기에 맞는 **Animation Clip**이 **Override**되도록 구성했습니다.

- **Avatar Mask**, **Blend Tree** 등을 활용하여 캐릭터 이동과 애니메이션이 자연스럽게 보이도록 구성했습니다.

### 5. UI 자동화
<details open>
  <summary>DB 매니저 코드 일부</summary>

```csharp
//DBManager.cs
public bool TryGet<T>(int key, out T value) where T : ScriptableObject
{
    value = null;

    if (typeof(T) == typeof(WeaponSO))
    {
        if (GetFrom(weaponDB, key, out WeaponSO weaponSO))
        {
            value = weaponSO as T;
            return true;
        }

        return false;
    }

    ...
}
```
</details>

- 캐릭터, 무기, 맵 데이터를 기반으로 로비에서 **DBManager**에 등록된 데이터를 참조해 선택 버튼을 자동 생성하는 UI 구조를 구현했습니다.

- **ScriptableObject**와 **Dictionary**를 활용하여 콘텐츠가 추가되더라도 UI 수정 비용이 적도록 구성했습니다.

## 6. 트러블 슈팅
### 1. 시야 시스템 구현
- 문제 상황: 처음에는 Material과 Shader를 활용하여 시야 시스템을 구현하려 했지만, 시야 범위 밖 오브젝트가 제대로 처리되지 않거나 모든 오브젝트에 별도 Material 처리가 필요해 확장성과 유지보수 측면에서 한계가 있었습니다.

- 해결 방식: **FOV 전용 카메라**와 **Shader Graph**, **Render Texture**, **FullScreen Pass**를 조합하여 시야 영역만 별도로 마스킹하도록 구성하였고, 환경 오브젝트 역시 카메라별 렌더 설정을 분리해 FOV 카메라에서도 시야가 가려지는 영역이 반영되도록 구현했습니다.

- 결과: 시야 범위 표현과 환경 오브젝트에 의한 시야 가림 효과가 자연스럽게 반영되도록 개선하였으며, 이후 벽 뒤 캐릭터 실루엣 표현과 같은 방식으로도 응용할 수 있는 기반을 마련했습니다.

### 2. 클라이언트 간 투사체/이펙트 판정 동기화 문제 해결
- 문제 상황: 총 발사 시 적용되는 탄 퍼짐 방향이 각 클라이언트마다 다르게 계산되어, 화면상으로는 빗나가 보이지만 실제 판정은 맞는 등 전투 피드백이 서로 다르게 보이는 문제가 있었습니다.

- 해결 방식: RPC로 발사 신호만 전달하던 구조에서, 각 클라이언트가 actorNumber, fireSequence, bulletIndex 조합을 기준으로 동일한 Spread 비율과 퍼짐 각도를 계산하도록 변경하여 **같은 발사 방향이 재현**되도록 구성했습니다.

- 결과: 각 클라이언트에서 보이는 탄 퍼짐 방향과 실제 판정의 차이를 줄였으며, 전투 상황에서 체감되는 피격 피드백의 일관성을 높이도록 개선했습니다.

### 3. 3D 사운드(SoundListener) 방향 및 시점 불일치 해결
- 문제 상황: 기존에는 SoundListener를 로컬 플레이어 오브젝트에 직접 부착하여 사운드 방향이 카메라 기준이 아니라 캐릭터 회전 기준으로 들리면서, 화면에서 보는 방향과 실제 들리는 방향이 어긋나는 문제가 있었습니다.

- 해결 방식: SoundListener를 별도 오브젝트로 분리하고 위치는 플레이어를 따라가도록 유지하되, 회전값은 카메라 기준으로 고정하도록 구조를 변경하여 **사운드가 들리는 방향과 화면 방향이 일치**하도록 구성했습니다.

- 결과: 사운드의 좌우 방향감이 화면 기준으로 더 자연스럽게 들리도록 개선하였으며, 이후 관전 시점이나 다른 대상 추적 구조에도 확장할 수 있는 방향을 확보했습니다.
