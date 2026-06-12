# Class Diagrams

This document contains structural class diagrams depicting the Domain entities and relationships in the Galvão system.

---

## 1. Domain Entities Structural Diagram

The following class diagram models the core domain models defined in the `Galvao.Domain` layer, showcasing attributes, methods, inheritance, and cardinality.

```mermaid
classDiagram
    class BaseEntity {
        <<abstract>>
        +Guid Id
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +bool Active
        #BaseEntity()
        #BaseEntity(Guid id)
        +Update()
        +Activate()
        +UnActivate()
    }

    class Member {
        +string DisplayName
        +string Email
        +string FirstName
        +string LastName
        +bool AcceptNews
        +bool AcceptPromo
        -Member()
        -Member(email, displayName, firstName, lastName, acceptNews, acceptPromo)
        +Create(email, displayName, firstName, lastName, acceptNews, acceptPromo)$ Result~Member~
        +UpdateProfile(displayName, firstName, lastName) Result
        +UpdateMarketingPreferences(acceptNews, acceptPromo) Result
        +UpdateEmail(email) Result
    }

    class MemberContact {
        +string ExternalContactId
        +string Email
        +bool Unsubscribed
        -MemberContact()
        -MemberContact(memberId, externalContactId, email, unsubscribed)
        +Create(memberId, externalContactId, email, unsubscribed)$ Result~MemberContact~
        +UpdateStatus(unsubscribed)
        +UpdateContactDetails(externalContactId, email) Result
    }

    class ShowroomItem {
        +string Title
        +string Description
        +decimal Price
        +string Category
        +IReadOnlyCollection~ShowroomItemPhoto~ Photos
        -List~ShowroomItemPhoto~ _photos
        -ShowroomItem()
        -ShowroomItem(title, description, price, category)
        +Create(title, description, price, category)$ Result~ShowroomItem~
        +UpdateDetails(title, description, price, category) Result
        +AddPhoto(url, caption, isPrimary) Result~ShowroomItemPhoto~
        +RemovePhoto(photoId) Result
        +SetPrimaryPhoto(photoId) Result
    }

    class ShowroomItemPhoto {
        +Guid ShowroomItemId
        +string Url
        +string Caption
        +bool IsPrimary
        -ShowroomItemPhoto()
        -ShowroomItemPhoto(showroomItemId, url, caption, isPrimary)
        +Create(showroomItemId, url, caption, isPrimary)$ Result~ShowroomItemPhoto~
        +SetAsPrimary()
        +ClearPrimary()
    }

    class Article {
        +string Title
        +string Content
        +string Author
        +DateTime? PublishedAt
        +bool IsPublished
        -Article()
        -Article(title, content, author)
        +Create(title, content, author)$ Result~Article~
        +UpdateContent(title, content, author) Result
        +Publish() Result
        +Unpublish() Result
    }

    BaseEntity <|-- Member
    BaseEntity <|-- MemberContact
    BaseEntity <|-- ShowroomItem
    BaseEntity <|-- ShowroomItemPhoto
    BaseEntity <|-- Article

    Member "1" *-- "1" MemberContact : Shares Identity Key / Cascade
    ShowroomItem "1" *-- "*" ShowroomItemPhoto : Contains / Backing Field
```

### Class Model Specification
* **BaseEntity**: The foundation class containing record identifiers and auditing fields (`CreatedAt`, `UpdatedAt`). It uses C# 12 `Guid.CreateVersion7()` to produce sequential, database-friendly UUIDs for primary keys.
* **Member**: Holds profile details. Has a strict one-to-one relationship mapped in the database with both:
  * The ASP.NET Core Identity `Account` (which holds username, email hash, and security tokens).
  * The `MemberContact` (which records CRM newsletter segments and unsubscribe flag).
* **MemberContact**: Tracks external marketing states. By sharing the primary key with `Member`, it guarantees integrity while avoiding composite navigation lookup overheads.
* **ShowroomItem**: Exposes showroom items. Controls access to photos via an encapsulated read-only collection `IReadOnlyCollection<ShowroomItemPhoto>`, backed by a private list field `_photos`. Mapped in EF Core using `PropertyAccessMode.Field`.
* **ShowroomItemPhoto**: Models image URLs. Mapped via cascade delete to their parent `ShowroomItem`.
* **Article**: Business model representing news content. Exposes behavior endpoints (`Publish()`, `Unpublish()`) to mutate publication state constraints.
