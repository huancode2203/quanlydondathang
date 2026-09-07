import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Customer, SaveCustomer } from '../../core/models/master-data.model';
import { AuthService } from '../../core/services/auth.service';
import { MasterDataApiService } from '../../core/services/master-data-api.service';

@Component({selector:'app-customer-management',standalone:true,imports:[ReactiveFormsModule],templateUrl:'./customer-management.component.html',styleUrl:'../shared/master-data.scss'})
export class CustomerManagementComponent {
  private readonly api=inject(MasterDataApiService);private readonly fb=inject(FormBuilder);readonly auth=inject(AuthService);
  readonly items=signal<Customer[]>([]);readonly dialogOpen=signal(false);readonly editingId=signal<number|null>(null);readonly saving=signal(false);readonly error=signal('');
  keyword='';
  readonly form=this.fb.nonNullable.group({code:['',Validators.required],name:['',Validators.required],phone:[''],email:['',[Validators.email]],address:[''],taxCode:[''],note:['']});
  constructor(){this.load()}
  load(){this.api.getCustomers(this.keyword).subscribe({next:x=>this.items.set(x),error:e=>this.error.set(this.readError(e))})}
  openCreate(){this.editingId.set(null);this.form.reset({code:`KH${String(Date.now()).slice(-5)}`});this.dialogOpen.set(true)}
  openEdit(x:Customer){this.editingId.set(x.id);this.form.reset({code:x.code,name:x.name,phone:x.phone??'',email:x.email??'',address:x.address??'',taxCode:x.taxCode??'',note:x.note??''});this.dialogOpen.set(true)}
  save(){if(this.form.invalid){this.form.markAllAsTouched();return}const value=this.form.getRawValue() as SaveCustomer;const id=this.editingId();const op=id?this.api.updateCustomer(id,value):this.api.createCustomer(value);this.saving.set(true);op.pipe(finalize(()=>this.saving.set(false))).subscribe({next:()=>{this.dialogOpen.set(false);this.load()},error:e=>this.error.set(this.readError(e))})}
  remove(x:Customer){if(confirm(`Xóa khách hàng ${x.code}?`))this.api.deleteCustomer(x.id).subscribe({next:()=>this.load(),error:e=>this.error.set(this.readError(e))})}
  private readError(e:unknown){return e instanceof HttpErrorResponse?e.error?.message??'Không thể xử lý yêu cầu.':'Đã xảy ra lỗi.'}
}
