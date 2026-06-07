import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import {
  AdminCategoriesRepository,
  AdminCurrenciesRepository,
  AdminUsersRepository,
} from './domain/ports/admin.repository';
import { AuthRepository } from './domain/ports/auth.repository';
import { CategoriesRepository } from './domain/ports/categories.repository';
import { CurrenciesRepository } from './domain/ports/currencies.repository';
import { DashboardRepository } from './domain/ports/dashboard.repository';
import { EntriesRepository } from './domain/ports/entries.repository';
import { MonthsRepository } from './domain/ports/months.repository';
import { authInterceptor } from './infrastructure/auth/auth.interceptor';
import {
  HttpAdminCategoriesRepository,
  HttpAdminCurrenciesRepository,
  HttpAdminUsersRepository,
} from './infrastructure/http/http-admin.repository';
import { HttpAuthRepository } from './infrastructure/http/http-auth.repository';
import { HttpEntriesRepository } from './infrastructure/http/http-entries.repository';
import { HttpCategoriesRepository, HttpCurrenciesRepository } from './infrastructure/http/http-lookups.repository';
import {
  HttpDashboardRepository,
  HttpMonthsRepository,
} from './infrastructure/http/http-months-dashboard.repository';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideCharts(withDefaultRegisterables()),
    { provide: AuthRepository, useClass: HttpAuthRepository },
    { provide: EntriesRepository, useClass: HttpEntriesRepository },
    { provide: MonthsRepository, useClass: HttpMonthsRepository },
    { provide: DashboardRepository, useClass: HttpDashboardRepository },
    { provide: CategoriesRepository, useClass: HttpCategoriesRepository },
    { provide: CurrenciesRepository, useClass: HttpCurrenciesRepository },
    { provide: AdminUsersRepository, useClass: HttpAdminUsersRepository },
    { provide: AdminCategoriesRepository, useClass: HttpAdminCategoriesRepository },
    { provide: AdminCurrenciesRepository, useClass: HttpAdminCurrenciesRepository },
  ],
};
