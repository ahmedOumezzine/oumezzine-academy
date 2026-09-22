# Phase 2 application boundary

The Application project owns framework-neutral query and command ports, transport-neutral DTOs, and capability abstractions. Its only project dependency is Domain. EF-backed and MVC-facing service implementations remain in Web until Infrastructure and controller-mapping phases.

## Service inventory

| Existing service | Phase 2 disposition | Boundary |
| --- | --- | --- |
| `StudyCatalogService` (`CourseCatalogService`, `LearningPathCatalogService`) | Split | EF queries and MVC ViewModel projection stay in Web; Application defines catalog and learning-path query ports with DTO results. |
| `QuizService` | Split | EF retrieval stays in Web; Application defines quiz retrieval/grading data, answer submission, and result DTOs. |
| `CoursePrerequisiteValidationService` | Split | Cycle detection orchestration now lives in Application; the EF edge adapter and compatibility service remain in Web. |
| `CourseVisibilityPolicy` | Split | EF expression remains in Web. The result type is Application-owned; localized display reasons stay in Web pending presentation mapping. |
| `DeleteCourseResult` | Move | Framework-neutral delete outcome now belongs to Application. |
| `HtmlSanitizerService` | Split | `IHtmlSanitizer` remains an Application port; the HtmlSanitizer-backed implementation belongs to Infrastructure. |
| `StudyMediaStorageService` | Split | Application exposes `IMediaStorage` and `MediaUpload`; `IFormFile` and filesystem implementation remain in Web. |
| `IStudyLmsCacheInvalidator` / `StudyLmsCacheInvalidator` | Split | Port belongs to Application; output-cache implementation and compatibility interface remain in Web. |
| `CurrentLanguageService` | Split | Application exposes `ICurrentLanguage`; culture-reading implementation and compatibility interface remain in Web. |
| `StudyLmsCacheKeys` | Leave in Web | Output-cache tags are host implementation details. |
| `StudyLocalization`, `LocalizedUrlService` | Leave in Web | Resource lookup, route culture, and URL construction are presentation/HTTP concerns. |
| `StudyLmsSeedService` | Defer to Infrastructure | Seeding is persistence-backed. |
| `AdminCategoryService`, `AdminCourseService` | Split | EF implementations and MVC models stay in Web; Application defines category/course command ports and request records. |
| `AdminChapterService`, `AdminLessonService` | Split | EF implementations and MVC models stay in Web; Application defines chapter/lesson command ports and request records. |
| `AdminLearningPathCategoryService`, `AdminLearningPathService` | Split | EF implementations and MVC models stay in Web; Application defines learning-path command port and request record. |
| `AdminQuizService`, `AdminQuestionService` | Split | EF implementations and MVC models stay in Web; Application defines quiz/question command ports and request records. |

The admin command ports establish the target boundary; concrete services do not implement them yet because their existing signatures accept MVC ViewModels and directly coordinate EF. Phase 3 can implement these ports without moving controller models into Application.
