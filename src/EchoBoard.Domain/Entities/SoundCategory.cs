namespace EchoBoard.Domain.Entities;

public sealed class SoundCategory
{
    private SoundCategory()
    {
    }

    private SoundCategory(Guid soundId, Guid categoryId)
    {
        SoundId = soundId;
        CategoryId = categoryId;
    }

    public Guid SoundId { get; private set; }

    public Guid CategoryId { get; private set; }

    public static SoundCategory Create(Guid soundId, Guid categoryId)
    {
        if (soundId == Guid.Empty)
        {
            throw new ArgumentException("A sound id is required.", nameof(soundId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A category id is required.", nameof(categoryId));
        }

        return new SoundCategory(soundId, categoryId);
    }
}
