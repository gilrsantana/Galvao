namespace Galvao.Presentation.Requests.Showroom;

public record UpdateShowroomItemPhotoRequest(
    string Url,
    string Caption,
    bool IsPrimary);
