import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';
import { TaskService } from '../../core/services/task.service';
import { TaskFormComponent } from './task-form.component';

describe('TaskFormComponent', () => {
  let taskService: jasmine.SpyObj<TaskService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    taskService = jasmine.createSpyObj<TaskService>('TaskService', ['createTask', 'updateTask', 'getTask']);
    taskService.createTask.and.returnValue(of({
      id: 'task-123',
      title: 'Release checklist',
      description: '',
      status: 'Todo',
      dueDate: '2030-01-01T04:59:59.000Z',
      createdUtc: '2029-01-01T00:00:00Z',
      userId: 'user-123'
    }));
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [TaskFormComponent],
      providers: [
        { provide: TaskService, useValue: taskService },
        { provide: Router, useValue: router },
        { provide: Location, useValue: { back: jasmine.createSpy('back') } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}) } } }
      ]
    }).compileComponents();
  });

  it('converts the selected due date using local end-of-day time', () => {
    const fixture = TestBed.createComponent(TaskFormComponent);
    const component = fixture.componentInstance;
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const selectedDate = [
      tomorrow.getFullYear(),
      String(tomorrow.getMonth() + 1).padStart(2, '0'),
      String(tomorrow.getDate()).padStart(2, '0')
    ].join('-');

    component.form.setValue({
      title: 'Release checklist',
      description: '',
      status: 'Todo',
      dueDate: selectedDate
    });
    component.submit();

    expect(taskService.createTask).toHaveBeenCalledOnceWith({
      title: 'Release checklist',
      description: '',
      dueDate: new Date(`${selectedDate}T23:59:59`).toISOString()
    });
  });
});
