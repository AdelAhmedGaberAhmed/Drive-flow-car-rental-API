# DriveFlow

DriveFlow is a car rental management system. A rental company uses it to keep track of its customers, its cars, every booking, every payment, and every car that comes back.

The app is used by the company's administrator. Customers don't log in. They give their details to the administrator, who enters everything in the app.

## How the app works

1. **Register the customer.** The administrator adds the customer's name, phone, email and driver's license number. Each license number is stored only once.
2. **Find a car.** The administrator filters the fleet by category and fuel type, and sees only the cars that are free for the requested dates.
3. **Create the booking.** The system calculates the number of days and the total price, saves the booking as confirmed, and records the first payment.
4. **Cancel or pick up.** A booking that has not been picked up can be cancelled, with the full amount refunded automatically. At pickup, the administrator records the date, the mileage and the condition of the car. The booking becomes active and the car is marked as rented.
5. **Return the car.** The administrator records the return date, the mileage, any extra charges (damage, cleaning, late return, fuel) and whether the car needs repair. The system works out the final bill in one step.
6. **Settle the bill.** If the customer paid too much, the refund is recorded automatically. If the customer still owes money, the booking stays active until the balance is paid. When nothing is left to pay, the booking is completed.
7. **Maintain the fleet.** A car that needs repair is taken out of rental until the repair is finished and the administrator confirms it is ready. Cars under maintenance cannot be booked.
8. **Review the numbers.** Reports show bookings, payments, vehicle status, returns and maintenance for any date range.

## Benefits

- **No double bookings.** A car cannot be booked for dates that overlap an existing booking.
- **Correct money, every time.** The price is saved with each booking, so a later price change does not affect it. Refunds and balances are calculated the same way for every customer.
- **One step for the return.** The return, the charges and the final bill are saved together. If one part fails, nothing is saved, so the records always match.
- **Cars are never rented while damaged.** Maintenance blocks bookings until the car is confirmed ready.
- **Clean records.** Duplicate customers (same license) and duplicate cars (same plate) are rejected. Old bookings and payments are kept, never deleted, so the history is complete.
- **Clear reports.** The owner can see how many bookings were made, how much money came in, how many cars are free, and what repairs cost.
- **Works with a mobile app.** The system provides a web API, so a Flutter app on phones or tablets can use it.
