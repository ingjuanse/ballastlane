import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { TaskService } from '../../core/services/task.service';
import { TaskListComponent } from './task-list.component';

describe('TaskListComponent', () => {
  let fixture: ComponentFixture<TaskListComponent>;
  let taskService: jasmine.SpyObj<TaskService>;

  beforeEach(async () => {
    taskService = jasmine.createSpyObj<TaskService>('TaskService', ['getTasks', 'deleteTask']);
    taskService.getTasks.and.returnValue(of({
      items: [{
        id: 'task-123',
        title: 'Release checklist',
        description: '',
        status: 'Todo',
        dueDate: '2030-01-01T00:00:00Z',
        createdUtc: '2029-01-01T00:00:00Z',
        userId: 'user-123'
      }],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 1
    }));
    taskService.deleteTask.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [TaskListComponent],
      providers: [
        provideRouter([]),
        { provide: TaskService, useValue: taskService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskListComponent);
    fixture.detectChanges();
  });

  it('asks before deleting and reloads the list after deletion', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    const task = fixture.componentInstance.tasks[0];

    fixture.componentInstance.deleteTask(task);

    expect(taskService.deleteTask).toHaveBeenCalledOnceWith('task-123');
    expect(taskService.getTasks).toHaveBeenCalledTimes(2);
  });

  it('does not delete when the user cancels the confirmation', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    fixture.componentInstance.deleteTask(fixture.componentInstance.tasks[0]);

    expect(taskService.deleteTask).not.toHaveBeenCalled();
    expect(taskService.getTasks).toHaveBeenCalledTimes(1);
  });
});
