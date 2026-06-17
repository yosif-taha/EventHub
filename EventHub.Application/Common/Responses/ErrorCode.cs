using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Common.Responses
{
    public enum ErrorCode
    {
        None = 0,

        // --- General ---
        [Description("An unexpected error occurred.")]
        UnhandledException = 100,

        [Description("One or more validation errors occurred.")]
        ValidationError = 101,

        [Description("An internal server error occurred. Please try again later.")]
        InternalServerError = 102,

        [Description("The requested value cannot be null.")]
        NullValue = 103,

        [Description("You are not authorized to access this resource.")]
        UnAuthorized = 104,

        [Description("You do not have permission to perform this action.")]
        Forbidden = 105,

        // --- Auth ---
        [Description("User not found.")]
        UserNotFound = 200,

        [Description("This email is already registered.")]
        UserAlreadyExist = 201,

        [Description("Invalid email or password.")]
        InvalidCredentials = 202,

        [Description("Please confirm your email address to activate your account.")]
        EmailNotConfirmed = 203,

        [Description("Password is too weak. Use a mix of letters, numbers, and symbols.")]
        WeakPassword = 204,

        [Description("Session expired or invalid refresh token. Please log in again.")]
        InvalidRefreshToken = 205,

        [Description("Account locked due to multiple failed attempts. Try again later.")]
        UserAccountLocked = 206,

        [Description("The specified role was not found.")]
        RoleNotFound = 207,

        [Description("A database error occurred. Please try again.")]
        DatabaseError = 208,

        [Description("This email address is already confirmed.")]
        EmailAlreadyConfirmed = 209,

        // --- Events ---
        [Description("The requested event was not found.")]
        EventNotFound = 300,

        [Description("This event has already been canceled.")]
        EventAlreadyCanceled = 301,

        [Description("The event date is invalid.")]
        EventInvalidDate = 302,

        [Description("The event has reached its maximum capacity.")]
        EventCapacityFull = 303,

        [Description("You do not have access to this private event.")]
        EventPrivateAccessDenied = 304,

        [Description("An event with this title already exists.")]
        EventTitleAlreadyExists = 305,

        [Description("An event with this title Is Full .")]
        EventIsFull = 306,

        [Description("High booking volume is currently unavailable, please try again.")]
        ConcurrencyConflict = 307,

        // --- Registration ---
        [Description("Event registration not found.")]
        RegistrationNotFound = 400,

        [Description("You are already registered for this event.")]
        AlreadyRegistered = 401,

        [Description("Registration for this event is now closed.")]
        RegistrationClosed = 402,

        [Description("The selected ticket type was not found.")]
        TicketTypeNotFound = 403,

        [Description("The promo code entered is invalid or expired.")]
        InvalidPromoCode = 404,

        [Description("This registration is already canceled.")]
        RegistrationAlreadyCanceled = 405,

        // --- Payments ---
        [Description("Payment processing failed. Please try again.")]
        PaymentFailed = 500,

        [Description("The payment provider encountered an error.")]
        PaymentProviderError = 501,

        [Description("Insufficient funds to complete the transaction.")]
        InsufficientFunds = 502,

        [Description("Refund is not allowed for this transaction.")]
        RefundNotAllowed = 503,

        // --- Categories ---
        [Description("The specified category was not found.")]
        CategoryNotFound = 600,

        [Description("Cannot delete category because it is currently in use.")]
        CategoryInUse = 601,

        [Description("Cannot Add category because it is currently Exist.")]
        CategoryAlreadyExist = 602,

        // --- Account/Profile ---
        [Description("Failed to update profile information.")]
        ProfileUpdateFailed = 700,

        [Description("The old password provided is incorrect.")]
        InvalidOldPassword = 701,

        [Description("Failed to upload the image. Please try again.")]
        ImageUploadFailed = 702
    }
}
