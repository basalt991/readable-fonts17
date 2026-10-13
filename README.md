# Readable Fonts 1.7

림월드 1.7의 UI 폰트를 Windows에 설치된 **맑은 고딕 굵게**로 교체하는 모드입니다.

**Windows / RimWorld 1.7 전용.** 각 사용자의 PC에 설치된 폰트를 읽습니다. 폰트 파일은 배포하지 않습니다.

## 다운로드와 설치

1. [최신 릴리스](https://github.com/basalt991/readable-fonts17/releases/latest)에서 `Readable-Fonts-1.7.zip`을 다운로드합니다.
2. 게임을 종료한 뒤 ZIP 안의 `Readable Fonts 1.7` 폴더를 림월드 설치 폴더의 `Mods` 안에 넣습니다. `Mods/Readable Fonts 1.7/About/About.xml` 구조가 되어야 합니다.
3. [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)를 설치하고 아래 사용 순서대로 활성화합니다.

설치된 맑은 고딕 굵게가 없으면 모드 설정에서 현재 게임 언어를 지원하는 다른 폰트를 선택합니다. 기본 폰트 적용에 실패하면 게임 원래 폰트를 복원합니다.

## English

Replaces the three standard RimWorld UI fonts with a font installed on your own Windows PC. Supports **RimWorld 1.7 only** and requires Harmony. The default is **Malgun Gothic Bold**; the settings provide an automatic installed-font list, search, previews, and a filter for the current game language. TTF, OTF, TTC, and OTC fonts are supported. No font files, game assemblies, or Harmony assemblies are bundled.

Download the ZIP from the [latest release](https://github.com/basalt991/readable-fonts17/releases/latest), extract its `Readable Fonts 1.7` folder into the game's `Mods` directory, and enable Harmony before this mod. Use the mod settings to choose another installed font or restore the original fonts. Settings are available in Korean and English. Glyph coverage is checked; complex-script shaping is not validated. In-game visual verification is still pending.

## 사용

1. 모드 목록에서 **Harmony**, **Readable Fonts 1.7**을 활성화합니다. Harmony 뒤에 배치합니다.
2. 게임을 다시 시작하면 기본값인 맑은 고딕 굵게가 적용됩니다.
3. **옵션 → 모드 설정 → 폰트 교체**를 열면 설치된 폰트 목록이 자동으로 표시됩니다. 이름으로 검색하고 폰트를 클릭한 뒤 **적용**을 누릅니다.
4. 기본적으로 **현재 게임 언어 지원 폰트만 보기**가 켜져 있습니다. 한국어로 플레이하면 한글 지원 폰트, 영어로 플레이하면 영문 지원 폰트가 표시됩니다. 게임 언어를 바꾸면 목록이 해당 언어 기준으로 다시 갱신됩니다. 필터를 끄면 모든 폰트가 표시됩니다. 새로운 폰트를 설치했다면 **새로 고침**을 누릅니다.
5. **기본 폰트로 되돌리기**를 누르면 교체 기능이 꺼집니다. 다시 사용하려면 **폰트 교체 사용**에 체크하고 **적용**을 누릅니다.

파일 경로를 입력할 필요가 없습니다. Windows의 시스템/사용자 폰트 등록 정보와 폰트 폴더를 조회하고, 폰트 내부에 기록된 이름과 굵기를 표시합니다. 폰트 파일은 모드에 포함하지 않습니다. 기본값은 계속 맑은 고딕 굵게이며 이전 버전에서 저장한 선택도 유지됩니다.

목록은 설정 화면을 처음 열 때 백그라운드에서 읽고 이후에는 캐시를 사용합니다. 이름이 긴 항목은 줄임표로 표시하고 마우스를 올리면 전체 이름과 현재 언어에 필요한 문자의 지원 수를 보여줍니다. 미리 보기의 언어도 함께 바뀝니다.

언어 판정에는 **현재 게임 언어의 기본 문자와 바닐라 UI 번역에 실제로 쓰인 글자**를 사용합니다. 한국어는 한글 완성형 11,172자, 일본어는 가나와 UI에 쓰인 한자, 중국어는 해당 번역에 쓰인 한자를 확인합니다. 유럽 언어의 악센트 문자, 키릴 문자와 다른 언어의 번역 문자도 확인합니다. 번역 태그, 자리 표시자와 모드의 다국어 예문은 제외합니다. 읽을 수 없는 문자 매핑을 가진 폰트는 전체 목록에는 남지만 언어 지원으로 표시하지 않습니다.

지원 표시는 글리프 유무를 기준으로 합니다. 아랍어 등의 문자 조합 동작은 엔진의 표시 방식에 따르며 이 검사에 포함되지 않습니다.

TTF/OTF와 TTC/OTC 글꼴 모음을 지원합니다. 바탕·바탕체·궁서·궁서체처럼 하나의 TTC 파일에 들어 있는 글꼴도 별개 항목으로 표시됩니다. 선택한 글꼴만 엔진이 읽을 수 있는 독립 폰트 데이터로 추출합니다.

## 적용 범위

- 림월드의 작은 글자, 일반 글자, 제목에 사용하는 세 가지 기본 UI 폰트를 교체합니다.
- 기존 Font 객체의 엔진 인덱스를 바꾸므로 같은 객체를 참조하는 입력란과 복제된 GUIStyle에도 반영됩니다.
- 원래 글자 크기를 유지하며 새 폰트에 맞게 줄 높이와 텍스트 높이 캐시를 갱신합니다.
- 글자를 부드럽게 표시하면서 힌팅을 적용합니다. 원래 UI 배율은 변경하지 않습니다.
- 자체 폰트, TextMeshPro, 이미지에 그려진 글자를 사용하는 UI는 적용 대상이 아닙니다.
- 다른 폰트 교체 모드와 동시에 켜면 적용 순서에 따라 결과가 달라질 수 있습니다.
- 파일이 없거나 폰트 등록에 실패하면 교체 전 폰트를 복원하고 모드 설정과 로그에 오류를 표시합니다.

## 호환성과 검증

- 대상: Windows, RimWorld **1.7.4976 rev1043**, Harmony 1.7용 어셈블리.
- `UnityEngine.Font.CreateDynamicFontFromOSFont`는 현재 1.7에서 실제 폰트를 읽지 않으므로 사용하지 않습니다.
- `UnityStubs.Gui.IGuiBackend.RegisterFont`로 폰트 파일을 직접 등록합니다. 엔진 등록 캐시 충돌을 피하려고 파일 해시와 크기 구분을 이름에 포함합니다.
- 빌드: 오류 0개, 경고 0개.
- 실제 Waylith 런타임/FreeType로 폰트 등록, 등록 캐시, 잘못된 폰트 거부, 한글 폭/높이 계산, 기존 Font 객체 변경과 복제된 GUIStyle의 복원을 독립 프로세스에서 확인했습니다.
- 현재 PC의 폰트 목록에서 402개 글꼴과 한글 완성형 전체를 지원하는 43개 글꼴을 확인했습니다. 목록 조회, 한국어 이름, 검색, 언어별 필터, 언어 변경 후 캐시 갱신, 중복 제거, TTC 분리/체크섬/문자 매핑 유지 검사 40개를 통과했습니다. 개수는 PC와 설치된 폰트 및 현재 번역에 따라 달라집니다.
- 배포 DLL을 실제 .NET 10 런타임에서 불러와 같은 402개 글꼴을 조회했습니다. 바탕 TTC의 네 글꼴을 각각 추출해 엔진에 등록하고, 사용자 폴더의 나눔바른고딕 Bold OTF도 실제 엔진에서 등록·측정·복원 검사를 통과했습니다.
- 실제 게임 어셈블리의 LoadedLanguage에 테스트 데이터를 넣어 현재 언어 탐지, 언어 전환, 프로필 캐시와 바닐라/모드 번역 구분을 독립 프로세스에서 확인했습니다. 한국어·영어·일본어·간체/번체 중국어·러시아어·독일어 필터 검사는 `Validation/language_filter_report.json`에 기록했습니다.
- 맑은 고딕 굵게의 한글 완성형 11,172자 포함 여부와 한국어/영어 설정 키 및 XML을 별도 검증합니다. 결과는 `Validation/qa_report.json`에 기록합니다.
- **게임에서 모드를 활성화한 뒤 메뉴, 설정, 정보 창과 실제 플레이 화면의 가독성/잘림 여부를 확인하는 검사는 아직 남아 있습니다.** 독립 엔진 검사는 게임 플레이 검사를 대신하지 않습니다.

## 빌드

`build.ps1`을 실행합니다. 설치된 게임의 .NET 10/UnityStubs 어셈블리를 직접 참조합니다. 이 환경에서는 .NET 9 SDK의 컴파일러로 빌드하되 런타임 참조는 게임에 포함된 .NET 10을 사용합니다. DLL만 `1.7/Assemblies`에 배포하며 게임 DLL과 Harmony DLL을 포함하지 않습니다.

`RimWorldDir`, `HarmonyPath` 매개변수로 설치 경로를 바꿀 수 있습니다. 림월드의 업데이트로 폰트 API가 바뀌면 재검증이 필요합니다.
