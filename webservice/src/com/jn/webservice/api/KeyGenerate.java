package com.jn.webservice.api;

import java.io.File;
import java.io.FileInputStream;

public class KeyGenerate {
	public void execute(String sessionID) {
//		String[] cd = {"/bin/bash", "cd", "/usr/java/easy-rsa"};
//		String[] source = {"source", "./vars"};
//		String[] clean= {"sh", "./clean-all"};
//		String[] var= {"sh", "./vars"};
//		String[] tool= {"sh", "./pkitool --initca client-"+sessionID};
		
		try{
			Process pb = Runtime.getRuntime().exec("sh /usr/java/easy-rsa/script " + sessionID); 
			pb.waitFor();
			
			Thread.sleep(50);
		}catch (Exception ee){
			ee.printStackTrace();
		}
		
//		try{
//			Process pb = Runtime.getRuntime().exec("source ./vars");
//			pb.waitFor();
//			
//			Thread.sleep(10);
//		}catch (Exception ee){
//			ee.printStackTrace();
//		}
//
//		try{
//			Process pb = Runtime.getRuntime().exec("sh ./clean-all");
//			pb.waitFor();
//			
//			Thread.sleep(10);
//		}catch (Exception ee){
//			ee.printStackTrace();
//		}
//		
//		try{
//			Process pb = Runtime.getRuntime().exec("sh ./vars");
//			pb.waitFor();
//			
//			Thread.sleep(10);
//		}catch (Exception ee){
//			ee.printStackTrace();
//		}
//		
//		try{
//			Process pb = Runtime.getRuntime().exec("sh ./pkitool --initca client-"+sessionID);
//			pb.waitFor();
//			
//			Thread.sleep(50);
//		}catch (Exception ee){
//			ee.printStackTrace();
//		}
	}
	
	public String getKey(String filename){
		String result = "";
		try{
			File file = new File("/usr/java/easy-rsa/keys/" + filename);
			byte data[] = new byte[(int)file.length()];
			
			FileInputStream fis = new FileInputStream(file);
			fis.read(data);
			fis.close();
			
			result = new String(data);
		}catch (Exception e){
			e.printStackTrace();
		}
		
		return result;
	}
}
