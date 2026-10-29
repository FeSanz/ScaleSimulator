namespace ScaleSim.Core.Abstractions;

/// <summary>The scale models this build knows how to simulate.</summary>
public sealed class ScaleCatalog
{
    private readonly List<IScaleModel> _models = new();

    public IReadOnlyList<IScaleModel> Models => _models;

    public ScaleCatalog Register(IScaleModel model)
    {
        if (_models.Any(m => m.Info.Id == model.Info.Id))
            throw new InvalidOperationException($"Model '{model.Info.Id}' is already registered.");
        _models.Add(model);
        return this;
    }

    public IScaleModel? Find(string id) => _models.FirstOrDefault(m => m.Info.Id == id);
}
