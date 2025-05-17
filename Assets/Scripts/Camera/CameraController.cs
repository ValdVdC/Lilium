using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCamera;           // Referência à câmera principal (controlada pelo Cinemachine)
    public Camera tablePuzzleCamera;     // Câmera dedicada para a mesa de puzzle
    public Camera clockPuzzleCamera;     // Câmera dedicada para o puzzle do relógio
    public Camera shelfPuzzleCamera;     // Câmera dedicada para o puzzle da estante de livros
    public Camera barrelCamera;          // Câmera dedicada para barris
    public Camera paintingCamera;        // Câmera dedicada para quadros

    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool transitionInProgress = false;
    private Camera activeCamera;

    // Câmeras para diferentes interações
    private enum CameraType
    {
        Main,
        TablePuzzle,
        ClockPuzzle,
        ShelfPuzzle,
        Painting,
        Barrel
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Configurar câmeras de interação
        SetupInteractionCameras();
        
        // Ativar apenas a câmera principal no início
        SetActiveCamera(CameraType.Main);
    }

    private void SetupInteractionCameras()
    {
        // Configurar câmera da mesa de puzzle
        if (tablePuzzleCamera == null)
        {
            GameObject tableCamObj = new GameObject("Table Puzzle Camera");
            tablePuzzleCamera = tableCamObj.AddComponent<Camera>();
            tablePuzzleCamera.orthographic = true;
            tablePuzzleCamera.depth = mainCamera.depth + 1; // Maior profundidade para renderizar na frente
        }
        tablePuzzleCamera.gameObject.SetActive(false);

        if (clockPuzzleCamera == null)
        {
            GameObject clockCamObj = new GameObject("Clock Puzzle Camera");
            clockPuzzleCamera = clockCamObj.AddComponent<Camera>();
            clockPuzzleCamera.orthographic = true;
            clockPuzzleCamera.depth = mainCamera.depth + 1;
        }
        clockPuzzleCamera.gameObject.SetActive(false);

        if (shelfPuzzleCamera == null)
        {
            GameObject shelfCamObj = new GameObject("Shelf Puzzle Camera");
            shelfPuzzleCamera = shelfCamObj.AddComponent<Camera>();
            shelfPuzzleCamera.orthographic = true;
            shelfPuzzleCamera.depth = mainCamera.depth + 1;
        }
        shelfPuzzleCamera.gameObject.SetActive(false);

        // Configurar câmera dos barris
        if (barrelCamera == null)
        {
            GameObject barrelCamObj = new GameObject("Barrel Camera");
            barrelCamera = barrelCamObj.AddComponent<Camera>();
            barrelCamera.orthographic = true;
            barrelCamera.depth = mainCamera.depth + 1;
        }
        paintingCamera.gameObject.SetActive(false);
                if (paintingCamera == null)
        {
            GameObject paintingCamObj = new GameObject("Painting Camera");
            paintingCamera = paintingCamObj.AddComponent<Camera>();
            paintingCamera.orthographic = true;
            paintingCamera.depth = mainCamera.depth + 1;
        }
        paintingCamera.gameObject.SetActive(false);
        
        // Copiar settings relevantes da main camera
        CopyCameraSettings(mainCamera, tablePuzzleCamera);
        CopyCameraSettings(mainCamera, clockPuzzleCamera);
        CopyCameraSettings(mainCamera, shelfPuzzleCamera);
        CopyCameraSettings(mainCamera, barrelCamera);
        CopyCameraSettings(mainCamera, paintingCamera);
        
        activeCamera = mainCamera;
    }
    
    private void CopyCameraSettings(Camera source, Camera target)
    {
        // Copiar configurações relevantes sem substituir posição ou rotação
        target.clearFlags = source.clearFlags;
        target.backgroundColor = source.backgroundColor;
        target.cullingMask = source.cullingMask;
        target.orthographicSize = source.orthographicSize;
        target.farClipPlane = source.farClipPlane;
        target.nearClipPlane = source.nearClipPlane;
        
        // Preservar a tag da câmera principal, se necessário
        if (source.tag == "MainCamera")
            target.tag = "Untagged"; // Evitamos ter múltiplas câmeras com tag MainCamera
    }

    private void SetActiveCamera(CameraType cameraType)
    {
        // Desativar todas as câmeras primeiro
        mainCamera.gameObject.SetActive(false);
        tablePuzzleCamera.gameObject.SetActive(false);
        clockPuzzleCamera.gameObject.SetActive(false);
        shelfPuzzleCamera.gameObject.SetActive(false);
        barrelCamera.gameObject.SetActive(false);
        paintingCamera.gameObject.SetActive(false);

        // Ativar a câmera selecionada
        switch (cameraType)
        {
            case CameraType.Main:
                mainCamera.gameObject.SetActive(true);
                activeCamera = mainCamera;
                break;
            case CameraType.TablePuzzle:
                tablePuzzleCamera.gameObject.SetActive(true);
                activeCamera = tablePuzzleCamera;
                break;
            case CameraType.Barrel:
                barrelCamera.gameObject.SetActive(true);
                activeCamera = barrelCamera;
                break;
            case CameraType.Painting:
                paintingCamera.gameObject.SetActive(true);
                activeCamera = paintingCamera;
                break;
            case CameraType.ClockPuzzle:
                clockPuzzleCamera.gameObject.SetActive(true);
                activeCamera = clockPuzzleCamera;
                break;
            case CameraType.ShelfPuzzle:
                shelfPuzzleCamera.gameObject.SetActive(true);
                activeCamera = shelfPuzzleCamera;
                break;
        }
    }

    // Ativar câmera da mesa de puzzle e configurá-la para olhar para a mesa
    public void ActivateTablePuzzleCamera(Transform puzzlePosition, float zoomLevel)
    {
        if (transitionInProgress)
            return;

        Vector3 tableCamPosition = puzzlePosition.position;
        tableCamPosition.z = -10; // Manter Z negativo para câmera 2D
        
        tablePuzzleCamera.transform.position = tableCamPosition;
        tablePuzzleCamera.orthographicSize = zoomLevel;
        
        StartCoroutine(FadeBetweenCameras(mainCamera, tablePuzzleCamera));
    }

    public void ActivateClockPuzzleCamera(Transform clockPosition, float zoomLevel)
    {
        if (transitionInProgress)
            return;

        Vector3 clockCamPosition = clockPosition.position;
        clockCamPosition.z = -10; // Manter Z negativo para câmera 2D
        
        clockPuzzleCamera.transform.position = clockCamPosition;
        clockPuzzleCamera.orthographicSize = zoomLevel;
        
        StartCoroutine(FadeBetweenCameras(mainCamera, clockPuzzleCamera));
    }

    public void ActivateShelfPuzzleCamera(Transform shelfPosition, float zoomLevel, string hexColor = "#110A07")
    {
        if (transitionInProgress)
            return;

        Vector3 shelfCamPosition = shelfPosition.position;
        shelfCamPosition.z = -10; // Manter Z negativo para câmera 2D
        
        shelfPuzzleCamera.transform.position = shelfCamPosition;
        shelfPuzzleCamera.orthographicSize = zoomLevel;

        Color backgroundColor;
        if (ColorUtility.TryParseHtmlString(hexColor, out backgroundColor))
            shelfPuzzleCamera.backgroundColor = backgroundColor;
        else
            shelfPuzzleCamera.backgroundColor = Color.black; // Cor padrão se a conversão falhar
        
        StartCoroutine(FadeBetweenCameras(mainCamera, shelfPuzzleCamera));
    }

    // Ativar câmera do barril e configurá-la para olhar para o barril específico
    public void ActivateBarrelCamera(Transform barrelPosition, float zoomLevel)
    {
        if (transitionInProgress)
            return;

        Vector3 barrelCamPosition = barrelPosition.position;
        barrelCamPosition.z = -10; // Manter Z negativo para câmera 2D
        
        barrelCamera.transform.position = barrelCamPosition;
        barrelCamera.orthographicSize = zoomLevel;
        
        StartCoroutine(FadeBetweenCameras(mainCamera, barrelCamera));
    }

    // Ativar câmera do quadro e configurá-la para olhar para o quadro específico
    public void ActivatePaintingCamera(Transform paintingPosition, float zoomLevel)
    {
        if (transitionInProgress)
            return;

        Vector3 paintingCamPosition = paintingPosition.position;
        paintingCamPosition.z = -10; // Manter Z negativo para câmera 2D
        
        paintingCamera.transform.position = paintingCamPosition;
        paintingCamera.orthographicSize = zoomLevel;
        
        StartCoroutine(FadeBetweenCameras(mainCamera, paintingCamera));
    }

    // Voltar para a câmera principal
    public void ReturnToMainCamera()
    {
        if (transitionInProgress)
            return;

        Camera currentCamera = activeCamera;
        StartCoroutine(FadeBetweenCameras(currentCamera, mainCamera));
    }

    private IEnumerator FadeBetweenCameras(Camera fromCamera, Camera toCamera)
    {
        transitionInProgress = true;

        // Criar um efeito de fade temporário
        GameObject fadeObj = new GameObject("CameraFade");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Garantir que fica na frente de tudo
        
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = Color.black;
        fadeImage.rectTransform.sizeDelta = new Vector2(Screen.width * 2, Screen.height * 2);

        // Fade out
        float startTime = Time.time;
        while (Time.time - startTime < transitionDuration * 0.5f)
        {
            float t = (Time.time - startTime) / (transitionDuration * 0.5f);
            fadeImage.color = new Color(0, 0, 0, t);
            yield return null;
        }

        // Trocar câmeras
        fromCamera.gameObject.SetActive(false);
        toCamera.gameObject.SetActive(true);
        activeCamera = toCamera;

        // Fade in
        startTime = Time.time;
        while (Time.time - startTime < transitionDuration * 0.5f)
        {
            float t = 1 - (Time.time - startTime) / (transitionDuration * 0.5f);
            fadeImage.color = new Color(0, 0, 0, t);
            yield return null;
        }

        // Limpar
        Destroy(fadeObj);
        transitionInProgress = false;
    }

    // Método para obter a câmera atual
    public Camera GetCurrentCamera()
    {
        // Retorna a câmera ativa atual
        return activeCamera;
    }
}