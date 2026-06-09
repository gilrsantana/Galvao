namespace Galvao.Presentation.Requests.Showroom;

public record AddShowroomItemPhotoRequest(
    string Url,
    string Caption,
    bool IsPrimary);
