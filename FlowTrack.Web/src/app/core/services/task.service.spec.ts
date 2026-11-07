import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends search and status filters with the paged task request', () => {
    service.getTasks(2, 10, 'Todo', 'release plan').subscribe();

    const request = http.expectOne(request => request.url === '/api/tasks');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    expect(request.request.params.get('status')).toBe('Todo');
    expect(request.request.params.get('search')).toBe('release plan');
    request.flush({ items: [], pageNumber: 2, pageSize: 10, totalCount: 0 });
  });

  it('deletes a task by id', () => {
    service.deleteTask('task-123').subscribe();

    const request = http.expectOne('/api/tasks/task-123');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
