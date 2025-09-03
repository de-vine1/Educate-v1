# 📌 Phase 1 – Course \& Level Management

A. Admin Panel (Backend-first)

1\. Data Model Design



We need 3 main entities with relationships:



Course



CourseId (GUID, PK)



Name (e.g., “ATS Examination”, “ICAN Examination”)



Description (overview of the course)



CreatedAt, UpdatedAt



Level (belongs to a course)



LevelId (GUID, PK)



CourseId (FK) → links to Course



Name (e.g., ATS1, ATS2, ATS3, Foundation, Skills, Professional)



Order (e.g., 1, 2, 3 for sequence)



CreatedAt, UpdatedAt



Subject (belongs to a level)



SubjectId (GUID, PK)



LevelId (FK) → links to Level



Name (e.g., “Communication Skills”, “Corporate Law”)



CreatedAt, UpdatedAt



📌 Relationships:



Course → Levels (One-to-Many)



Level → Subjects (One-to-Many)



2\. Admin Features (Controllers \& Endpoints)



Course Management



Create, update, delete courses.



View all courses (paginated list).



Example: POST /api/admin/courses



Level Management



Add levels under a course.



Update level name/order.



Delete levels (with constraints: must handle dependent subjects).



Example: POST /api/admin/courses/{courseId}/levels



Subject Management



Add subjects under a level.



Update subject names.



Delete subjects (constraint: exams/questions under subject must be handled).



Example: POST /api/admin/levels/{levelId}/subjects



3\. Admin Panel UI/UX Flow



Dashboard → "Manage Courses" tab.



Add/Edit/Delete Courses.



Within a course, manage its Levels.



Within each level, manage Subjects.



Tree-like structure:



Course → Level → Subjects

ICAN → Foundation → Corporate Law

&nbsp;                      Management Accounting

&nbsp;                      Business Environment

&nbsp;                      Financial Accounting



4\. Validation \& Security



Only Admins (Role-Based Access Control via JWT Claims).



Prevent duplicate Course/Level/Subject names under the same hierarchy.



Audit logs for all create/update/delete actions.



B. User-Facing Side (Later Phase)

1\. Course Discovery



Users can view all available professional courses (ATS, ICAN, etc.).



Each course displays:



Course name \& description.



Levels with subjects (expandable).



2\. Enrollment \& Subscription



User selects a Course + Level.



Redirected to payment process (Paystack/Monnify).



On success → record created in UserCourses:



UserCourseId



UserId (FK)



CourseId (FK)



LevelId (FK)



SubscriptionStartDate



SubscriptionEndDate (6 months validity)



Status (Active/Expired/Pending)



3\. User Profile Integration



Users see:



Subscribed Courses \& Levels.



Subjects under each level.



Expiry date of each subscription.



Renew button → leads back to payment flow.



C. Example with Your Provided Data

ATS Examination



ATS1



Communication Skills



Basic Accounting



Economics



Business Law



ATS2



Financial Accounting



Public Sector Accounting



Quantitative Analysis



Information Technology



ATS3



Cost Accounting



Taxation



Management



Principles of Auditing



ICAN Examination



Foundation



Corporate Law



Management Accounting



Business Environment



Financial Accounting



Skills



Financial Reporting



Audit, Assurance \& Forensics



Taxation



Performance Management



Financial Management



Public Sector Accounting \& Finance



Professional



Strategic Business Reporting



Advanced Taxation



Case Study



Strategic Financial Management



Advanced Audit \& Assurance



D. Notifications \& Renewals



System sends:



Email + in-app notification when subscription is about to expire (7 days \& 1 day before).



Renewal link included in notification.



If expired → user can no longer access exams/materials under that Level until renewal.





📌 Phase 2 – User Enrollment \& Subscription Flow
===

A. Core Concept



Users can enroll in multiple professional courses.



Enrollment is tied to a specific course + level (not the entire course at once).



Subscriptions are bi-annual (6 months validity).



After expiry, users must renew to continue access.



Users can have multiple active subscriptions across different courses and levels.



B. Data Model Design



UserCourses Table (junction table between User, Course, Level)



UserCourseId (GUID, PK)



UserId (FK → Users)



CourseId (FK → Courses)



LevelId (FK → Levels)



SubscriptionStartDate



SubscriptionEndDate (+6 months)



Status (Pending, Active, Expired, Cancelled)



PaymentId (FK → Payments)



Payments Table



PaymentId (GUID, PK)



UserId (FK)



Amount



Provider (Paystack/Monnify)



Reference (unique payment reference)



Status (Pending, Success, Failed)



CreatedAt



C. User Flow (Step-by-Step)

1\. Course + Level Selection



User browses available courses \& levels (from Phase 1).



Example: User selects ICAN → Foundation.



2\. Enrollment Request



User clicks “Subscribe”.



Backend creates:



Payments record (Status = Pending).



UserCourses record (Status = Pending).



Redirects to payment gateway (Paystack/Monnify).



3\. Payment Processing



User completes payment on gateway.



Gateway sends webhook notification to backend.



Backend:



Verifies transaction via API.



Updates Payments.Status = Success.



Updates UserCourses.Status = Active.



Sets SubscriptionStartDate = now, SubscriptionEndDate = +6 months.



4\. Post-Enrollment



User is redirected to “Enrollment Success” page.



Email receipt + subscription details sent via SendGrid.



In-app dashboard updates to show:



Active subscription(s).



Expiry dates.



Remaining validity (countdown).



D. Renewal Flow



Notifications



7 days before expiry → email + in-app: “Your subscription for ICAN Foundation expires in 7 days. Click here to renew.”



1 day before expiry → reminder.



On expiry → status changes to Expired.



Renewal Action



User clicks Renew.



System initializes a new Payment record.



If successful → update existing UserCourses with new SubscriptionEndDate = +6 months.



E. Edge Cases



Multiple Subscriptions: A user can subscribe to ATS1 and ICAN Foundation at the same time. Both have independent expiry dates.



Upgrade Path: If a user finishes ATS1 and wants ATS2 → they must enroll and pay separately.



Failed Payment: If payment fails, UserCourses.Status remains Pending. User can retry.



Overlapping Renewals: If user renews early, extend SubscriptionEndDate by 6 months from current expiry, not from today.



F. Notifications \& Receipts



Email Receipt (after payment):



Course + Level



Amount paid



Payment provider + reference



Subscription validity dates



In-app Dashboard:



List of all subscriptions with status (Active/Expired/Pending).



Renewal button.



Email Reminders:



Before expiry.



On expiry.



G. Security Considerations



API keys stored in dotnet user-secrets.



Validate webhook signatures for both Paystack \& Monnify.



Never trust frontend → always verify payments server-side.



JWT-secured endpoints:



POST /api/user/enroll



GET /api/user/subscriptions



POST /api/user/renew



H. Flow Diagram

User → Select Course + Level → Backend creates Payment + UserCourse(Pending) → Redirect to Gateway

→ Payment Gateway → Webhook → Verify Payment → Update DB (Success) → UserCourse(Active) → Notify User





Phase 3 – Payments Integration (Monnify + Paystack)
===



This phase ensures users can securely pay for their subscriptions (ATS/ICAN levels) using either Monnify or Paystack. It covers setup, payment initialization, webhook validation, receipts, and subscription linking.



3.1. Payment Providers Setup



API Keys Management



Store Paystack and Monnify keys in .NET user-secrets (not in codebase).



Example:



dotnet user-secrets set "Paystack:SecretKey" "your\_secret\_key\_here"

dotnet user-secrets set "Monnify:ApiKey" "your\_api\_key\_here"

dotnet user-secrets set "Monnify:SecretKey" "your\_secret\_key\_here"





Configuration Classes



Create PaystackConfig and MonnifyConfig classes.



Inject via IOptions<T> in Startup/Program.cs for DI.



3.2. Payment Initialization



Flow:



User selects exam → level → subscription plan (6 months).



User chooses payment provider (Paystack or Monnify).



Backend generates transaction request and sends to provider API.



Provider returns payment link → frontend redirects user.



Paystack InitializeTransaction API



Required fields: amount, email, reference, callback\_url.



Save transactionReference in DB with status = Pending.



Monnify One-Time Payment API



Required fields: amount, customerName, customerEmail, paymentReference.



Save paymentReference in DB with status = Pending.



3.3. Payment Webhooks (Verification Layer)



Webhook Endpoints



/api/payments/paystack/webhook



/api/payments/monnify/webhook



Security



Validate webhook signatures:



Paystack: Check x-paystack-signature against request body + secret key (HMAC SHA512).



Monnify: Validate with hashed secret key as per docs.



Reject if invalid.



Verification



On webhook hit, verify transaction status via provider API (never trust webhook alone).



Update transaction record:



Status = Success → Mark subscription as Active (6 months).



Status = Failed → Mark as Failed, notify user.



3.4. Receipts \& Record-Keeping



Database Tables



Payments



PaymentId, UserId, ExamId, LevelId, Provider, Amount, Reference, Status, CreatedAt, UpdatedAt.



Receipts



ReceiptId, PaymentId, ReceiptNumber, FilePath/PDFData, IssuedAt.



Receipt Generation



Use QuestPDF or iTextSharp in .NET to auto-generate PDF receipts.



Include: User details, exam/level, provider used, transaction reference, amount, date.



Store in DB or file system (depending on scale).



Delivery



Email receipt to user via SendGrid (or similar).



Also downloadable from user dashboard.



3.5. Subscription Linking



After successful payment → update:



UserSubscriptions table with:



UserId, ExamId, LevelId, StartDate, EndDate (6 months later), Status = Active.



If subscription expires → mark Expired + notify via email \& dashboard.



3.6. Notifications



Email



Payment successful → send confirmation + attached receipt.



Payment failed → send retry instructions.



In-App



Show subscription status in dashboard (Active, Expiring Soon, Expired).



Display renewal prompts before expiry (e.g., 2 weeks before).



3.7. Testing \& Monitoring



Test Environments



Use Paystack and Monnify sandbox/test environments for integration.



Verify success, failure, timeout, webhook replay scenarios.



Idempotency



Ensure repeated webhooks/requests don’t duplicate transactions.



Use unique reference for each payment.



Error Logging



Log failed payments, webhook rejections, invalid signatures.



Build admin dashboard to monitor payment stats.



✅ At the end of Phase 3:



Users can choose Monnify or Paystack.



Payments are securely processed and verified.



Receipts are auto-generated and stored.



Subscriptions are activated \& linked to user.



Notifications (email + in-app) keep users informed.






Phase 4 – Renewal \& Expiry Handling
===



This phase ensures users can renew their course subscriptions every 6 months, get proper notifications before and after expiry, and the system maintains accurate subscription statuses.



4.1. Subscription Lifecycle



Active → When user subscribes and payment is verified.



Expiring Soon → 14 days before expiry.



Expired → After end date passes and no renewal.



Renewed → When user pays again for the same course level before/after expiry.



4.2. Database Design



UserSubscriptions Table (continuing from Phase 3):



SubscriptionId (PK)



UserId (FK)



ExamId (ATS/ICAN)



LevelId



StartDate



EndDate (StartDate + 6 months)



Status (Active, ExpiringSoon, Expired, Renewed)



RenewalCount (integer)



Audit Trail (optional but recommended):



SubscriptionHistory table → log every renewal event with timestamps, payment reference, provider used.



4.3. Expiry \& Renewal Logic



Automatic Expiry



Background job (Hangfire / Quartz.NET) runs daily.



Checks subscriptions where EndDate <= Now.



Updates status → Expired.



Sends expiry email + in-app notification.



Expiring Soon Notifications



Background job checks for subscriptions expiring in 14 days.



Status → ExpiringSoon.



Send reminder: “Your ICAN Skills subscription is about to expire. Renew now to avoid losing access.”



Renewal Flow



User clicks Renew button in dashboard.



Redirected to choose provider (Paystack/Monnify).



New payment initialized (reference must be unique).



On success → extend EndDate by 6 months.



Increment RenewalCount.



Status → Renewed.



4.4. Notifications



Email Triggers



Expiring Soon (14 days before) → “Renew now” CTA.



On Expiry → “Your subscription has expired, click here to renew.”



On Renewal → Payment receipt + new subscription dates.



In-App Notifications



Show banners in dashboard.



Example: “ATS2 subscription expiring in 12 days. Renew now to continue learning.”



4.5. Payment Handling for Renewal



Re-use Phase 3 Payment Flow.



Difference:



Instead of creating a fresh subscription, check if an active/expired one exists for (UserId + ExamId + LevelId).



Extend EndDate by 6 months from current expiry.



If expired, set new StartDate = Now, EndDate = Now + 6 months.



4.6. Security \& Integrity



Prevent duplicate renewals:



Use idempotent payment references.



Block renewals if transaction status is still Pending.



Verify payments server-side (again via Monnify/Paystack API).



Log all renewal attempts for auditing.



4.7. Admin Panel Controls



Admin can:



See all active/expired subscriptions.



Manually extend subscriptions (e.g., goodwill cases).



View renewal history per user.



Export reports of renewals vs expiries.



4.8. Testing Scenarios



Normal Renewal: User renews before expiry → EndDate extended.



Late Renewal: User renews after expiry → New cycle starts from payment date.



Failed Payment: Renewal attempt fails → Status unchanged, reminder persists.



Webhook Replay: Ensure multiple webhook hits don’t create duplicate renewals.



Multiple Subscriptions: Same user can have ICAN + ATS running separately, each with own lifecycle.



✅ At the end of Phase 4:



Subscriptions auto-expire after 6 months.



Users get multiple reminders via email + in-app.



Renewals are smooth, secure, and linked to payment records.



Admins have full control \& visibility.




Phase 5 – User Dashboard \& Progress Tracking
===



This phase delivers the student-facing portal where they manage courses, monitor subscription status, access study materials, and track progress across different levels.



5.1. Dashboard Structure



When a user logs in, the dashboard should have:



Welcome Section – Personalized greeting with name.



Active Subscriptions Overview – Cards showing current courses \& expiry dates.



Notifications – Renewal reminders, exam tips, announcements.



Quick Actions – “Renew Subscription”, “Enroll in New Course”, “Download Materials”.



Progress Tracker – Shows which topics/levels have been completed.



Payment History \& Receipts – Access to past payments \& downloadable invoices.



5.2. User Subscriptions View



Display all active and expired subscriptions in a table or card format.



Course Name (e.g., ICAN – Skills)



Level (e.g., Audit, Assurance \& Forensics)



Status (Active, ExpiringSoon, Expired, Renewed)



Start \& End Dates



Renewal Button (if applicable)



5.3. Learning Access Control



Access Rules:



If subscription is Active/Renewed → user can access materials, practice tests, and progress tracking.



If subscription is Expired → user can view past progress but locked out of new content until renewal.



If subscription is ExpiringSoon → allow access but show banners urging renewal.



5.4. Progress Tracking



Course Levels \& Topics:



Each professional course has levels (ATS1, ATS2, ATS3, ICAN Foundation, Skills, Professional).



Each level has subjects (e.g., Communication Skills, Taxation, Strategic Business Reporting).



Each subject can be broken down into modules/chapters for fine-grained tracking.



Tracking Model:



UserProgress table:



ProgressId (PK)



UserId (FK)



CourseId (FK)



LevelId



SubjectId



CompletionStatus (Not Started, In Progress, Completed)



Score (optional, if tied to quizzes/tests)



LastAccessed (timestamp)



Visual Representation:



Progress bars per level.



Percentage completion per subject.



Timeline view of completed modules.



5.5. Study Materials Access



Materials stored in cloud storage (Azure Blob / AWS S3 / Google Cloud Storage).



Linked per subject (e.g., PDF notes, slides, recorded lectures).



Users with active subscriptions can:



View materials online.



Download limited copies (optionally controlled by admin).



Version control: when materials are updated, students see latest but can also access previous versions.



5.6. Quizzes \& Practice Tests



Optional but critical for later phases:



Each subject has a bank of practice questions.



Users can attempt mock tests (timed \& untimed).



Progress tracking integrates quiz performance.



Scores saved in UserProgress table.



Gamification: Badges for milestones (e.g., "Completed ATS2 Taxation").



5.7. Notifications in Dashboard



Expiring subscriptions banners.



“New Materials Added” alerts.



“Exam Date Reminders” (admin-scheduled).



Sync with email notifications from Phase 4 for consistency.



5.8. Payment History \& Receipts



Table of past transactions:



Provider (Paystack/Monnify).



Transaction reference.



Date of payment.



Status (Successful/Failed).



Download Receipt button (PDF).



Helps with transparency and builds trust.



5.9. Admin Panel Controls



Admin can:



View per-user progress (for insights \& analytics).



Reset progress (e.g., for retakes).



Upload/update study materials per subject/level.



Push notifications/announcements to user dashboards.



5.10. Security \& Privacy



Users can only access courses they are subscribed to.



Prevent URL manipulation (JWT authorization checks every request).



Role-based access control (Admin vs User).



All progress data tied strictly to UserId and course subscription.



5.11. Testing Scenarios



User with multiple active subscriptions (ATS1 + ICAN Foundation). → Dashboard should show both.



User with expired subscription → Progress visible but materials/tests locked.



Renewed subscription → Progress continues without reset.



Admin uploads new material → Users notified in dashboard + email.



User attempts quiz → Score stored and visible in progress tracker.



✅ At the end of Phase 5:



Users have a clear dashboard summarizing subscriptions, progress, payments, and notifications.



Learning access is controlled by subscription status.



Progress tracking adds value and improves retention.



Admins can manage content \& view analytics effectively.

# 

# 




Phase 6 – Practice Tests \& Mock Exams System
===



The goal of this phase is to let users simulate real exam conditions, attempt questions by level/subject, receive instant feedback, and track their performance over time.



6.1. Types of Tests



Practice Mode (Self-paced)



Users select a subject (e.g., ATS1 – Communication Skills).



They can answer one question at a time, get instant feedback, and view explanations.



No time limit.



Mock Exam Mode (Simulated Exam)



Users attempt a full-length exam with questions randomly drawn from a question bank.



Strict time limits (similar to ICAN/ATS real exam).



No feedback until submission.



At the end, user gets a scorecard + solutions review.



6.2. Question Bank Structure



Database Models:



QuestionBank table:



QuestionId (PK)



CourseId (FK)



LevelId (FK)



SubjectId (FK)



QuestionText (supports text, LaTeX for formulas, images if needed).



Options (JSON: A, B, C, D).



CorrectAnswer.



Explanation (why the answer is correct).



Difficulty (Easy, Medium, Hard).



CreatedBy (AdminId).



CreatedAt.



Admin Panel Features:



Upload questions (bulk via Excel/CSV or manual entry).



Tag questions by difficulty.



Enable/disable questions (in case of errors).



6.3. Test Generation



Practice Mode:



User selects subject → fetch X random questions from bank.



Can retry questions multiple times.



Mock Exam Mode:



Fetch randomized set of questions (e.g., 100).



Shuffle order.



Apply time limit (e.g., 3 hours).



Autosubmit when time runs out.



6.4. Test Attempt Flow



User selects Practice Test or Mock Exam.



System fetches questions \& starts session.



During test:



User selects answers.



In practice mode → immediate feedback (Correct/Incorrect + Explanation).



In mock exam → answers saved but feedback only after submission.



On submission (or auto-submit):



System calculates score.



Stores results in UserTestAttempts table.



6.5. Results \& Analytics



UserTestAttempts Table:



AttemptId



UserId



CourseId, LevelId, SubjectId



TestType (Practice/Mock)



Score



TotalQuestions



CorrectAnswers



WrongAnswers



AttemptDate



TimeTaken



User Dashboard Integration:



Score history over time (line chart).



Weak subjects highlighted (e.g., “You scored lowest in Taxation”).



Comparison of mock scores to pass mark thresholds.



6.6. Review \& Explanations



After each test, user sees:



Scorecard: % correct, time spent, ranking vs past attempts.



Question-by-question breakdown: User’s answer vs Correct answer + explanation.



Option to retry incorrect questions.



6.7. Gamification



Badges/Achievements:



“Completed First Mock Exam”



“Scored 80%+ in Foundation”



“Consistent Streak (3 tests in a row)”



Leaderboard (optional, if you want competitive element).



6.8. Notifications \& Engagement



Email \& dashboard reminders:



“You haven’t taken a test in 2 weeks”



“Your average score improved in Business Law!”



Encourage renewals by showing progress toward passing.



6.9. Security \& Integrity



Prevent cheating (especially for mock exams):



Shuffle questions \& options.



Limit copy-paste (frontend-level).



Server-side validation of answers (no client trust).



Store attempts securely, tied to UserId.



6.10. Testing Scenarios



User starts a mock but leaves halfway → system auto-saves progress.



User takes same subject multiple times → analytics show improvement/decline.



Admin updates a question (fix typo) → past attempts remain intact.



Expired subscription → block new tests, but past results visible.



✅ At the end of Phase 6:



Users can take both practice tests and real exam-style mocks.



Their results are tracked, analyzed, and visualized in the dashboard.



Admins manage question banks and exams.



Renewals are encouraged naturally via progress \& reminders.

