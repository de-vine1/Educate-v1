AI Coding Instructions for Educate Platform (Updated with Phases & Steps)
Architecture Overview

Clean Architecture .NET 9 educational platform.

Layers:

Educate.API: Web API controllers, middleware, extensions, configuration

Educate.Application: Application services, DTOs, interfaces

Educate.Domain: Entities, value objects, domain exceptions

Educate.Infrastructure: Data access, external services, implementations

Data Flow: Controller → Application Interface → Infrastructure Implementation → PostgreSQL Database

Core Domain Model

User: Extends IdentityUser
Fields: FirstName, LastName, Username, Email, OAuthProvider, Subscriptions, Payments, TestResults, CreatedAt, LastLoginAt, EmailConfirmedAt

Course → Level → Subject → Test

Subscription: Tracks user access and expiry (6 months)

Payment: Handles Paystack/Monnify payments

Test/QuestionBank: Manages practice tests and mock exams

Phases & Steps
Phase 1 – Course & Level Management (Admin Panel First)

Goal: Create course hierarchy so users can subscribe to levels.

Admin can create Courses (ATS, ICAN)

Admin can add Levels within courses:

ATS1, ATS2, ATS3

Foundation, Skills, Professional

Admin can add Subjects under levels:

Example: ATS1 → Communication Skills, Basic Accounting, Economics, Business Law

Example: ICAN Foundation → Corporate Law, Management Accounting

Admin can update, delete, or disable courses, levels, or subjects

Database Tables:

Courses → Levels → Subjects (one-to-many relationships)

Pros: Organized content, ready for subscription flow

Cons: Requires admin discipline for correct order

Phase 2 – User Enrollment & Subscription Flow

Goal: Allow users to register, enroll in courses, and manage subscriptions.

Registration Flow

Standard signup:

Fields: First Name, Last Name, Username, Email, Password, Confirm Password

Password hashed via ASP.NET Identity

Google OAuth signup

After OAuth signup, prompt to set password later

Email Verification

Send 6-digit confirmation code via SendGrid

Store token hashed in DB

Notify users if unverified on login (can verify later)

Subscriptions

User selects course and level

Subscription duration: 6 months

User can subscribe to multiple levels and courses

Notifications for expiry via email and dashboard

Database Tables:

Users

Subscriptions (UserId, CourseId, LevelId, StartDate, ExpiryDate, Status)

Pros: Flexible subscriptions, multi-course support

Cons: Requires proper notification handling for expiry

Phase 3 – Payments Integration (Monnify + Paystack)

Goal: Collect payments for subscriptions securely.

User selects a course level → chooses payment provider

Monnify: One-time payments

Paystack: Full transaction handling

Server-side Verification

Never trust frontend confirmation

Verify payment signature via webhook

Receipts

Generate PDF/email receipt after payment

Store in Payments table

Database Tables

Payments (UserId, SubscriptionId, Amount, Provider, Status, ReceiptUrl, CreatedAt)

Security Best Practices

Store API keys in dotnet user-secrets

HTTPS everywhere

Validate webhook signatures

Pros: Dual payment providers increase reliability

Cons: Webhook handling complexity

Phase 4 – Renewal & Expiry Handling

Goal: Manage subscription expiry and renewal reminders.

Background Service

SubscriptionBackgroundService runs periodic checks

Notifications

7 days before expiry: send email and dashboard notification

After expiry: block new tests but allow dashboard review

Renewal

User can renew subscription via payment

Extend expiry date by 6 months

Pros: Automatic reminders improve retention

Cons: Requires reliable background processing

Phase 5 – User Dashboard & Progress Tracking

Goal: Show subscribed courses, progress, and test scores.

Dashboard Elements

Active subscriptions

Expiry dates

Test history and scores

Weak subjects (performance analytics)

Progress Tracking

Track practice test completion per subject

Track mock exam scores

Pros: Users see performance trends

Cons: Requires relational queries across multiple tables

Phase 6 – Practice Tests & Mock Exams System

Goal: Provide learning and exam simulation.

Practice Mode

Self-paced, one question at a time

Instant feedback

Mock Exam Mode

Timed full exam, random question selection

Scorecard after submission

Question Bank Structure

Fields: QuestionText, Options, CorrectAnswer, Explanation, Difficulty

Admin can upload in bulk

Test Attempt Flow

Start test → save answers → calculate score → store in UserTestAttempts

Analytics

User progress over time

Weak subjects highlighted

Pros: Simulates real exam conditions

Cons: Complexity in randomization and scoring

Phase 7 – Admin Panel Advanced Features

Goal: Manage users, courses, payments, and analytics.

Bulk upload of courses, levels, subjects, and questions

View all users and subscriptions

View payments and generate receipts

Analytics dashboards:

Engagement

Subscription renewals

Test performance across subjects

Audit logging for all actions

Pros: Efficient platform management

Cons: Requires role-based security enforcement

Authentication & Security

JWT + Refresh Tokens

Login returns access and refresh tokens

Google OAuth

Optional signup, then prompt for password

Forgot Password

Send reset link via email (SendGrid)

Token stored hashed in DB

Password Hashing

ASP.NET Core Identity default hashing

Rate Limiting

IP-based throttling

Audit Logging

All critical actions logged

Pros: Secure, standard best practices

Cons: Complexity in token refresh handling

Database Overview

Tables:

Users (IdentityUser extended)

Courses → Levels → Subjects

Subscriptions (User, Course, Level, Start/Expiry)

Payments (User, Subscription, Amount, Provider)

UserTestAttempts (TestType, Score, TimeTaken)

QuestionBank (Questions, Options, CorrectAnswer, Difficulty)

AuditLogs (User, Action, Timestamp, IP, UserAgent)

Integration Points

SendGrid

Registration emails, password reset, notifications

Monnify & Paystack

Payment processing, webhook verification

JWT Tokens

Authentication & authorization

Background Services

Expiry & renewal notifications

Development Guidelines

Controllers: Use DTOs, async methods, include navigation properties

Services: Use dependency injection; implement interfaces in Infrastructure

Middleware: Security headers, request validation, audit logging, rate limiting

Database: PostgreSQL, migrations via EF Core

Secrets: Store SendGrid, Monnify, Paystack keys using dotnet user-secrets