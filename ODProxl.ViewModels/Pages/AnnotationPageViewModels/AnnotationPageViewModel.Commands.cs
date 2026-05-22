namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels;

public partial class AnnotationPageViewModel
{
    public AsyncDelegateCommand OpenImagesCommand { get; private set; }
    public DelegateCommand SetRectModeCommand { get; private set; }
    public DelegateCommand SetPolygonModeCommand { get; private set; }
    public DelegateCommand ResetZoomCommand { get; private set; }
    public DelegateCommand CancelPolygonCommand { get; private set; }
    public AsyncDelegateCommand PrevImageCommand { get; private set; }
    public AsyncDelegateCommand NextImageCommand { get; private set; }
    public DelegateCommand<Annotation> DeleteAnnotationCommand { get; private set; }
    public AsyncDelegateCommand AutoAnnotateCommand { get; private set; }
    public DelegateCommand AddNewClassCommand { get; private set; }
    public DelegateCommand OpenFileCommand { get; private set; }
    public AsyncDelegateCommand SaveAnnotationsCommand { get; private set; }
    public DelegateCommand ToRuleClassPageCommand { get; private set; }

    private void InitializeCommands()
    {
        OpenImagesCommand = new AsyncDelegateCommand(OpenImagesAsync);
        SetRectModeCommand = new DelegateCommand(() => IsPolygonMode = false);
        SetPolygonModeCommand = new DelegateCommand(() => IsPolygonMode = true);
        ResetZoomCommand = new DelegateCommand(() => RequestResetZoom?.Invoke());
        CancelPolygonCommand = new DelegateCommand(CancelCurrentPolygon);
        PrevImageCommand = new AsyncDelegateCommand(async () =>
        {
            if (CurrentImageIndex > 0) await LoadImageAsync(CurrentImageIndex - 1);
        });
        NextImageCommand = new AsyncDelegateCommand(async () =>
        {
            if (CurrentImageIndex < ImagePaths.Count - 1) await LoadImageAsync(CurrentImageIndex + 1);
        });
        DeleteAnnotationCommand = new DelegateCommand<Annotation>(ann =>
        {
            if (ann != null && Annotations.Contains(ann))
            {
                Annotations.Remove(ann);
                RedrawAllAnnotations();
            }
        });
        AutoAnnotateCommand = new AsyncDelegateCommand(RunAutoAnnotationAsync);
        AddNewClassCommand = new DelegateCommand(async () => await AddNewClassAsync());
        OpenFileCommand = new DelegateCommand(async () => await OpenPdfAsync());
        SaveAnnotationsCommand = new AsyncDelegateCommand(SaveAnnotationsToServerAsync);
        ToRuleClassPageCommand = new DelegateCommand(ToRuleClassPage);
    }
}