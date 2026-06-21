namespace Galvao.Domain.Base;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool Active { get; private set; }

    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
        CreatedAt = DateTime.Now;
        Active = true;
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.Now;
        Active = true;
    }



    public virtual void Update()
    {
        UpdatedAt = DateTime.Now;
    }

    public void Activate()
    {
        Active = true;
        Update();
    }

    public void UnActivate()
    {
        Active = false;
        Update();
    }
}
