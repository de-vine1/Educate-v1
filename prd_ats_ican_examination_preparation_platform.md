# Product Requirements Document (PRD)

## Product Name
ATS & ICAN Examination Preparation Platform

## Version
1.0

## Prepared For
Internal Product Discussion & Development Alignment

## Overview
This document defines the product requirements for a web-based examination preparation platform focused on **ATS (Accounting Technician Scheme)** and **ICAN (Institute of Chartered Accountants of Nigeria)** examinations. The platform will provide structured courses, controlled access through subscriptions, mock exams, progress tracking, and an admin-driven content management system.

The goal is to build a scalable, secure, and exam-focused learning platform that mirrors real examination structures while remaining simple for students and powerful for administrators.

---

## Problem Statement

### Current Problems
1. **Fragmented Learning Resources**
   Students preparing for ATS and ICAN exams rely on scattered materials (PDFs, WhatsApp groups, random videos) with no structured progression.

2. **Lack of Exam-Oriented Platforms**
   Existing learning platforms are generic and not aligned with ATS/ICAN syllabi, exam formats, or progression levels.

3. **Poor Progress Visibility**
   Students have no clear insight into how prepared they are, which subjects they are weak in, or how close they are to exam readiness.

4. **Manual Administration**
   Training centers and tutors manage students, payments, and renewals manually, leading to errors and poor scalability.

5. **Payment & Access Control Issues**
   No seamless way to tie payments to access duration, renewals, and course eligibility.

---

## Proposed Solution

Build a **role-based, subscription-driven exam preparation platform** that:
- Strictly follows ATS and ICAN examination structures
- Offers time-bound access to courses and mock exams
- Tracks student progress and readiness
- Automates payments, renewals, and access control
- Provides admins with deep visibility into student activity and platform performance

---

## Target Users

### Primary Users
- ATS Students (Levels 1–3)
- ICAN Students (Foundation, Skills, Professional)

### Secondary Users
- Platform Administrators
- Academic Content Managers
- Training Institutions (future expansion)

---

## Product Goals

1. Improve exam pass rates through structured preparation
2. Centralize ATS & ICAN learning into one platform
3. Automate student enrollment, access, and renewals
4. Provide measurable insights into student performance
5. Enable scalable management through a powerful admin panel

---

## Scope Summary

### In Scope
- Course & subject management
- User enrollment & subscriptions
- Payment integrations (Monnify & Paystack)
- Access expiry & renewal handling
- User dashboard & progress tracking
- Practice tests & mock exams
- Advanced admin analytics & management tools

### Out of Scope (Initial Phase)
- Live classes
- Video conferencing
- Mobile apps (future phase)
- AI-based tutoring (future phase)

---

## Examination Structure

### ATS Examination

**ATS 1**
- Communication Skills
- Basic Accounting
- Economics
- Business Law

**ATS 2**
- Financial Accounting
- Public Sector Accounting
- Quantitative Analysis
- Information Technology

**ATS 3**
- Cost Accounting
- Taxation
- Management
- Principles of Auditing

### ICAN Examination

**Foundation**
- Corporate Law
- Management Accounting
- Business Environment
- Financial Accounting

**Skills**
- Financial Reporting
- Audit, Assurance & Forensics
- Taxation
- Performance Management
- Financial Management
- Public Sector Accounting & Finance

**Professional**
- Strategic Business Reporting
- Advanced Taxation
- Case Study
- Strategic Financial Management
- Advanced Audit & Assurance

---

## Functional Requirements

## Phase 1 – Course & Level Management

### Objective
Create a structured academic hierarchy aligned with ATS and ICAN syllabi.

### Implementation Steps
1. Define examination bodies (ATS, ICAN)
2. Define levels under each body
3. Define subjects under each level
4. Assign metadata (exam body, level, subject, status)
5. Enable admin CRUD operations for all academic entities
6. Implement visibility controls (draft, published, archived)
7. Enforce strict relationships (subjects cannot exist outside levels)

---

## Phase 2 – User Enrollment & Subscription Flow

### Objective
Allow users to enroll into specific exam levels via subscriptions.

### Implementation Steps
1. User registration and authentication
2. Profile completion (exam type, target level)
3. Subscription plan mapping to exam levels
4. Enrollment validation before payment
5. Access provisioning after successful subscription
6. Role-based access enforcement

---

## Phase 3 – Payments Integration (Paystack)

### Objective
Enable secure and flexible payments for subscriptions.

### Implementation Steps
1. Configure Paystack as payment providers
2. Generate unique transactions per enrollment
3. Handle callbacks and payment verification
4. Prevent duplicate or replayed transactions
5. Tie payment records to subscriptions
6. Log payment status (pending, success, failed)

---

## Phase 4 – Renewal & Expiry Handling

### Objective
Ensure time-bound access with automated expiry and renewal.

### Implementation Steps
1. Define subscription duration rules
2. Calculate access start and end dates
3. Auto-expire access on due date
4. Restrict content upon expiry
5. Notify users before expiry
6. Handle renewal as subscription extension or reset

---

## Phase 5 – User Dashboard & Progress Tracking

### Objective
Give students visibility into their learning journey.

### Implementation Steps
1. Display enrolled exam level and subjects
2. Track completed lessons and tests
3. Calculate subject-wise progress
4. Highlight weak and strong areas
5. Show subscription status and expiry
6. Provide exam readiness indicators

---

## Phase 6 – Practice Tests & Mock Exams System

### Objective
Simulate real examination conditions.

### Implementation Steps
1. Create question banks per subject
2. Tag questions by difficulty and topic
3. Define mock exam rules (time, scoring)
4. Enforce exam conditions (timers, submission rules)
5. Auto-grade objective questions
6. Store attempt history
7. Provide performance analytics per attempt

---

## Phase 7 – Admin Panel Advanced Features

### Objective
Enable full operational control and insights for administrators.

### Implementation Steps

#### Analytics & Reporting
1. Track active users by exam type and level
2. Monitor subscription conversions and renewals
3. Analyze subject performance trends
4. Identify drop-off and completion rates
5. Export reports for management use

#### Bulk Operations
6. Bulk upload subjects and questions
7. Bulk student enrollment
8. Bulk subscription assignment
9. Bulk status updates (activate, suspend)

#### Student Management
10. View individual student profiles
11. Access enrollment and payment history
12. Monitor test attempts and progress
13. Manually adjust subscriptions if needed
14. Suspend or reinstate accounts

#### Content Governance
15. Approve or archive academic content
16. Version control for exam materials
17. Control visibility per cohort

---

## Backend Architecture Discussion Summary

### Core Backend Responsibilities
- Authentication & authorization
- Role-based access control
- Subscription lifecycle management
- Payment verification and reconciliation
- Academic content hierarchy enforcement
- Progress and performance tracking
- Admin analytics and reporting

### Key Backend Features
- Modular service-based structure
- Strong relational data integrity
- Secure webhook handling for payments
- Scheduled jobs for expiry handling
- Audit logs for admin actions
- Scalable design for future mobile and API clients

---

## Non-Functional Requirements

- High security for payments and user data
- Scalable architecture
- High availability during mock exams
- Clear audit trails
- Fast dashboard performance

---

## Success Metrics

- Subscription conversion rate
- Exam completion rates
- Mock exam participation
- Renewal percentage
- Student performance improvement

---

## Future Enhancements

- Mobile applications
- Live classes
- AI-powered performance recommendations
- Institution-level dashboards

---

## Conclusion

This PRD outlines a structured, exam-focused platform designed to solve real problems faced by ATS and ICAN students. By combining disciplined academic structure, automated subscriptions, realistic mock exams, and strong administrative controls, the platform positions itself as a serious preparation tool rather than a generic learning website.

